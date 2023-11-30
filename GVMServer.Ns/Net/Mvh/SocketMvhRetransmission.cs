namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using GVMServer.Ns.Enum;

    public enum SocketMvhRetransmissionState
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        AckUnknown,
        /// <summary>
        /// 确认成功
        /// </summary>
        AckSuccess,
        /// <summary>
        /// 确定重传
        /// </summary>
        AckRetransmission,
        /// <summary>
        /// 等待重传
        /// </summary>
        AckPending,
        /// <summary>
        /// 确认超时
        /// </summary>
        AckTimeout,
    }

    public class SocketMvhRetransmissionContext
    {
        /// <summary>
        /// 确认超时时间
        /// </summary>
        public int AckTimeout { get; set; }
        /// <summary>
        /// 最大重传次数
        /// </summary>
        public int MaxRetransmission { get; set; }
        /// <summary>
        /// 已经重传计数
        /// </summary>
        public int RetransmissionCount { get; internal set; }
        /// <summary>
        /// 重传消息
        /// </summary>
        public Message AckMessage { get; internal set; }
        /// <summary>
        /// 节点Id
        /// </summary>
        public Guid Id { get; internal set; }
        /// <summary>
        /// 节点类型
        /// </summary>
        public ApplicationType ApplicationType { get; internal set; }
        /// <summary>
        /// 重传事件
        /// </summary>
        public RetransmissionEvent RetransmissionEvent { get; internal set; }
        /// <summary>
        /// 重传状态
        /// </summary>
        public SocketMvhRetransmissionState RetransmissionState { get; internal set; }
        /// <summary>
        /// 重传链表节点
        /// </summary>
        internal LinkedListNode<SocketMvhRetransmissionContext> ackRetransmissionLinkedListNode;
        /// <summary>
        /// 上个重传时间
        /// </summary>
        internal DateTime ackRetransmissionWatchTime;
    }

    [UnmanagedFunctionPointer(CallingConvention.FastCall)]
    public delegate void RetransmissionEvent(SocketMvhRetransmissionContext context, Message message);

    public class SocketMvhRetransmission
    {
        private ConcurrentDictionary<string, SocketMvhRetransmissionContext> m_poAckRetransmissionTable =
            new ConcurrentDictionary<string, SocketMvhRetransmissionContext>();
        private LinkedList<SocketMvhRetransmissionContext> m_poAckRetransmissionLinkedList = new LinkedList<SocketMvhRetransmissionContext>();
        private LinkedListNode<SocketMvhRetransmissionContext> m_poCurrentAckRetransmissionLinkedListNode = null;
        private readonly Stopwatch m_poAckStopwatch = new Stopwatch();

        public const int DEFAULT_RETRANSMISSION_CONCURRENT = 100;

        public ISocketChannelManagement ChannelManagement { get; }

        public SocketMvhRetransmission(ISocketChannelManagement channelManagement)
        {
            this.ChannelManagement = channelManagement ?? throw new ArgumentNullException(nameof(channelManagement));
        }

        private string MeasureAckRetransmissionKey(ApplicationType applicationType, Guid id, Commands commands, long ackNo)
        {
            return $"{unchecked((int)applicationType)}.{id}.{unchecked((int)commands)}.{ackNo}";
        }

        private SocketMvhRetransmissionContext RemoveAckRetransmission(ApplicationType applicationType, Guid id, Commands commands, long ackNo)
        {
            string ackRetransmissionKey = MeasureAckRetransmissionKey(applicationType, id, commands, ackNo);
            if (string.IsNullOrEmpty(ackRetransmissionKey))
            {
                return null;
            }

            SocketMvhRetransmissionContext poRetransmissionContext = null;
            lock (m_poAckRetransmissionTable)
            {
                m_poAckRetransmissionTable.TryRemove(ackRetransmissionKey, out poRetransmissionContext);
                if (poRetransmissionContext != null)
                {
                    var poAckRetransmissionLinkedListNode = poRetransmissionContext.ackRetransmissionLinkedListNode;
                    var poAckRetransmissionLinkedList = poAckRetransmissionLinkedListNode.List;
                    if (poAckRetransmissionLinkedList != null)
                    {
                        if (m_poCurrentAckRetransmissionLinkedListNode == poAckRetransmissionLinkedListNode)
                        {
                            m_poCurrentAckRetransmissionLinkedListNode = m_poCurrentAckRetransmissionLinkedListNode.Next;
                        }
                        poAckRetransmissionLinkedList.Remove(poAckRetransmissionLinkedListNode);
                    }
                }
            }

            return poRetransmissionContext;
        }

        private SocketMvhRetransmissionContext MoveNextAckRetransmissionContext()
        {
            lock (m_poAckRetransmissionTable)
            {
                if (m_poAckRetransmissionLinkedList.Count <= 0)
                    m_poCurrentAckRetransmissionLinkedListNode = null;
                else
                {
                    if (m_poCurrentAckRetransmissionLinkedListNode == null)
                        m_poCurrentAckRetransmissionLinkedListNode = m_poAckRetransmissionLinkedList.First;
                    else
                        m_poCurrentAckRetransmissionLinkedListNode = m_poCurrentAckRetransmissionLinkedListNode.Next;
                }
                return m_poCurrentAckRetransmissionLinkedListNode?.Value;
            }
        }

        protected virtual bool IsAllowTickAlways()
        {
            lock (m_poAckStopwatch)
            {
                if (!m_poAckStopwatch.IsRunning)
                {
                    m_poAckStopwatch.Start();
                }

                double dblElapsedMilliseconds = m_poAckStopwatch.ElapsedTicks / 10000.00;
                if (double.IsNaN(dblElapsedMilliseconds) ||
                    double.IsInfinity(dblElapsedMilliseconds) ||
                    double.IsNegativeInfinity(dblElapsedMilliseconds) ||
                    double.IsPositiveInfinity(dblElapsedMilliseconds))
                {
                    return false;
                }

                if (dblElapsedMilliseconds < 10)
                {
                    return false;
                }

                m_poAckStopwatch.Restart();
            }
            return true;
        }

        public virtual int DoEvents(int maxConcurrent)
        {
            int iNextEventsOf = 0;
            if (maxConcurrent <= 0)
            {
                maxConcurrent = DEFAULT_RETRANSMISSION_CONCURRENT;
            }

            if (!this.IsAllowTickAlways())
            {
                return iNextEventsOf;
            }

            int iMaxConcurrent = 0;
            lock (m_poAckRetransmissionTable)
            {
                iMaxConcurrent = m_poAckRetransmissionLinkedList.Count;
                if (iMaxConcurrent <= maxConcurrent)
                {
                    iMaxConcurrent = maxConcurrent;
                }
            }

            if (iMaxConcurrent > 0)
            {
                DateTime stCurrentWathTime = DateTime.Now;
                for (; iNextEventsOf < iMaxConcurrent; iNextEventsOf++)
                {
                    SocketMvhRetransmissionContext poRetransmissionContext = MoveNextAckRetransmissionContext();
                    if (poRetransmissionContext == null)
                    {
                        break;
                    }

                    lock (poRetransmissionContext)
                    {
                        TimeSpan stElapsedTime = stCurrentWathTime - poRetransmissionContext.ackRetransmissionWatchTime;
                        if (stElapsedTime.TotalMilliseconds > poRetransmissionContext.AckTimeout)
                        {
                            RetransmissionMessage(poRetransmissionContext);
                        }
                    }
                }
            }

            return iNextEventsOf;
        }

        private bool CallEvent(SocketMvhRetransmissionContext poRetransmissionContext, SocketMvhRetransmissionState eRetransmissionState, Message message = null)
        {
            if (poRetransmissionContext == null)
            {
                return false;
            }

            lock (poRetransmissionContext)
            {
                poRetransmissionContext.RetransmissionState = eRetransmissionState;
                poRetransmissionContext.RetransmissionEvent?.Invoke(poRetransmissionContext, message);
                return true;
            }
        }

        private bool RetransmissionMessage(SocketMvhRetransmissionContext poRetransmissionContext)
        {
            if (poRetransmissionContext == null)
            {
                return false;
            }

            ISocketChannelManagement poChannelManagement = this.ChannelManagement;
            if (poChannelManagement == null)
            {
                return false;
            }

            lock (poRetransmissionContext)
            {
                if (poRetransmissionContext.RetransmissionCount >= poRetransmissionContext.MaxRetransmission)
                {
                    RemoveAckRetransmission(poRetransmissionContext.ApplicationType,
                        poRetransmissionContext.Id,
                        poRetransmissionContext.AckMessage.CommandId,
                        poRetransmissionContext.AckMessage.SequenceNo);
                    CallEvent(poRetransmissionContext, SocketMvhRetransmissionState.AckTimeout);
                    return false;
                }

                poRetransmissionContext.RetransmissionCount++;
                poRetransmissionContext.ackRetransmissionWatchTime = DateTime.Now;
                CallEvent(poRetransmissionContext, SocketMvhRetransmissionState.AckRetransmission);

                ISocket poSocket = poChannelManagement.GetChannel(poRetransmissionContext.ApplicationType, poRetransmissionContext.Id);
                if (poSocket == null)
                {
                    return false;
                }

                if (!poSocket.Send(poRetransmissionContext.AckMessage))
                {
                    return false;
                }

                return CallEvent(poRetransmissionContext, SocketMvhRetransmissionState.AckPending);
            }
        }

        public virtual bool AckRetransmission(ApplicationType applicationType, Guid id, Commands commands, long ackNo, Message message)
        {
            SocketMvhRetransmissionContext poRetransmissionContext = RemoveAckRetransmission(applicationType, id, commands, ackNo);
            return CallEvent(poRetransmissionContext, SocketMvhRetransmissionState.AckSuccess, message);
        }

        public virtual bool AddRetransmission(ApplicationType applicationType, Guid id, Message ackMessage, int ackTimeout,
            int ackRetransmission, RetransmissionEvent ackEvent, Commands ackCommands, out Exception exception)
        {
            exception = null;
            if (ackMessage == null)
            {
                exception = new ArgumentNullException("ackMessage is not allowed to be a null references");
                return false;
            }

            if (ackTimeout <= 0)
            {
                exception = new ArgumentOutOfRangeException("ackTimeout is not allowed to be less or equals than 0");
                return false;
            }

            if (ackRetransmission < 0)
            {
                exception = new ArgumentOutOfRangeException("ackRetransmission is not allowed to be less than 0");
                return false;
            }

            string ackRetransmissionKey = MeasureAckRetransmissionKey(applicationType, id, ackMessage.CommandId, ackMessage.SequenceNo);
            if (string.IsNullOrEmpty(ackRetransmissionKey))
            {
                exception = new ArgumentOutOfRangeException("Cannot be measured ackRetransmissionKey with the supplied parameter");
                return false;
            }

            lock (m_poAckRetransmissionTable)
            {
                if (m_poAckRetransmissionTable.TryGetValue(ackRetransmissionKey, out SocketMvhRetransmissionContext ackRetransmissionNode)
                    || ackRetransmissionNode != null)
                {
                    exception = new InvalidOperationException("Multiple messages with the same Id, ackNo, or command number are not allowed to be retransmitted");
                    return false;
                }

                SocketMvhRetransmissionContext poRetransmissionContext = new SocketMvhRetransmissionContext()
                {
                    MaxRetransmission = ackRetransmission,
                    ApplicationType = applicationType,
                    AckTimeout = ackTimeout,
                    AckMessage = ackMessage,
                    Id = id,
                    RetransmissionCount = -1,
                    RetransmissionEvent = ackEvent,
                    RetransmissionState = SocketMvhRetransmissionState.AckUnknown,
                    ackRetransmissionLinkedListNode = null,
                    ackRetransmissionWatchTime = DateTime.Now,
                };
                m_poAckRetransmissionTable[ackRetransmissionKey] = poRetransmissionContext;
                if (ackRetransmission > 0)
                {
                    poRetransmissionContext.ackRetransmissionLinkedListNode = m_poAckRetransmissionLinkedList.AddLast(poRetransmissionContext);
                }
                return RetransmissionMessage(poRetransmissionContext);
            }
        }
    }
}
