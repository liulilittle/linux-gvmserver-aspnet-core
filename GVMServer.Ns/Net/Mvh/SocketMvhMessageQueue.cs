namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using GVMServer.Log;
    using GVMServer.Ns.Deployment;

    public class SocketMvhMessageQueue : IDisposable
    {
        private class QueueContent
        {
            public ISocket socket;
            public Message message;
        }

        private class TickAlwaysContent
        {
            public Action<object> ticks;
            public object state;
        }

        private Thread[] m_aoWorkThreads;
        private bool m_disposed = false;
        private readonly LinkedList<QueueContent> m_poLinkedListQueue = new LinkedList<QueueContent>();
        private readonly ConcurrentDictionary<object, TickAlwaysContent> m_poTickAlwaysTable = new ConcurrentDictionary<object, TickAlwaysContent>();

        public ISocketChannelManagement ChannelManagement { get; }

        public SocketMvhMessageQueue(ISocketChannelManagement management, int concurrent)
        {
            this.ChannelManagement = management ?? throw new ArgumentOutOfRangeException(nameof(management));
            this.m_aoWorkThreads = new Thread[SystemEnvironment.ProcessorCount];
            for (int i = 0; i < this.m_aoWorkThreads.Length; i++)
            {
                Thread poWorkThread = new Thread(WorkThread)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                poWorkThread.Start();
                this.m_aoWorkThreads[i] = poWorkThread;
            }
        }

        ~SocketMvhMessageQueue()
        {
            this.Dispose();
        }

        public virtual bool AddMessage(ISocket socket, Message message)
        {
            if (message == null || socket == null)
            {
                return false;
            }
            lock (m_poLinkedListQueue)
            {
                var content = new QueueContent
                {
                    message = message,
                    socket = socket,
                };
                return null != m_poLinkedListQueue.AddLast(content);
            }
        }

        public virtual void AddTickAlways<T>(Action<T> ticks, T state)
        {
            if (ticks == null)
            {
                throw new ArgumentNullException(nameof(ticks));
            }

            lock (m_poTickAlwaysTable)
            {
                if (m_poTickAlwaysTable.ContainsKey(ticks))
                {
                    return;
                }

                var poContent = new TickAlwaysContent
                {
                    ticks = (obj) =>
                    {
                        if (obj == null || !(obj is T))
                        {
                            obj = default(T);
                        }
                        var xbox = unchecked((T)obj);
                        ticks(xbox);
                    },
                    state = state
                };

                m_poTickAlwaysTable.TryAdd(ticks, poContent);
            }
        }

        public virtual bool RemoveTickAlways<T>(Action<T> ticks)
        {
            if (ticks == null)
            {
                return false;
            }

            lock (m_poTickAlwaysTable)
            {
                return m_poTickAlwaysTable.TryRemove(ticks, out TickAlwaysContent poContent);
            }
        }

        public virtual void Dispose()
        {
            lock (m_poLinkedListQueue)
            {
                if (!m_disposed)
                {
                    m_disposed = true;
                    m_aoWorkThreads = null;
                    m_poLinkedListQueue.Clear();
                    m_poTickAlwaysTable.Clear();
                }
            }
            GC.SuppressFinalize(this);
        }

        protected virtual void TickAlways()
        {
            foreach (TickAlwaysContent poTickAlwaysContent in m_poTickAlwaysTable.Values)
            {
                if (poTickAlwaysContent == null)
                {
                    continue;
                }

                var state = poTickAlwaysContent.state;
                poTickAlwaysContent.ticks(state);
            }
        }

        protected virtual bool NextMessage()
        {
            QueueContent poQueueContent = null;
            lock (m_poLinkedListQueue)
            {
                var poNode = m_poLinkedListQueue.First;
                if (poNode != null)
                {
                    poQueueContent = poNode.Value;
                    m_poLinkedListQueue.Remove(poNode);
                }
            }

            if (poQueueContent == null)
            {
                return false;
            }

            OnMessage(poQueueContent.socket, poQueueContent.message);
            return true;
        }

        private void WorkThread()
        {
            while (!this.m_disposed)
            {
                this.TickAlways();
                int intSleepTime = SystemEnvironment.ClockSleepTime(m_aoWorkThreads?.Length ?? 0);
                if (!this.NextMessage())
                {
                    intSleepTime = intSleepTime <= 0 ? 1 : intSleepTime;
                }
                Thread.Sleep(intSleepTime);
            }
        }

        protected virtual void OnMessage(ISocket socket, Message message)
        {
            ISocketChannelManagement poChannelManagement = this.ChannelManagement;
            try
            {
                poChannelManagement.ProcessMessage(socket, message);
            }
            catch (Exception exception)
            {
                string stackTrace = LogController.CaptureStackTrace(exception, 1);
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    Console.WriteLine(stackTrace);
                }
            }
        }
    }
}
