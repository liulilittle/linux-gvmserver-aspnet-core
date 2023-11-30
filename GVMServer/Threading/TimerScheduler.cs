namespace GVMServer.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using GVMServer.Collection;

    public class TimerScheduler : IDisposable
    {
        private static readonly object _syncobj = new object();
        private static TimerScheduler _default = null;
        private Thread _mta;
        private bool _disposed;
        private LinkedList<Timer> _s;
        private LinkedListIterator<Timer> _i;
        private IDictionary<Timer, LinkedListNode<Timer>> _m;

        public static TimerScheduler Default
        {
            get
            {
                lock (_syncobj)
                {
                    if (_default == null)
                    {
                        _default = new TimerScheduler();
                    }
                    return _default;
                }
            }
        }

        public int Id
        {
            get
            {
                return _mta.ManagedThreadId;
            }
        }

        ~TimerScheduler()
        {
            this.Dispose();
        }

        public virtual int Count => _s.Count;

        public TimerScheduler()
        {
            _m = new Dictionary<Timer, LinkedListNode<Timer>>();
            _s = new LinkedList<Timer>();
            _i = new LinkedListIterator<Timer>(this, _s);
        }

        private bool Run()
        {
            lock (this)
            {
                if (_disposed)
                {
                    return false;
                }
                if (_mta != null)
                {
                    return false;
                }
                _mta = new Thread(() =>
                {
                    while (!_disposed)
                    {
                        for (; ; )
                        {
                            LinkedListNode<Timer> poCurrentNode = _i++.Node;
                            if (poCurrentNode == null)
                            {
                                break;
                            }

                            Timer poTimer = poCurrentNode.Value;
                            if (poTimer == null || !poTimer.Enabled)
                            {
                                continue;
                            }

                            TimeSpan tsDeltaTime = (DateTime.Now - poTimer.m_dtLastTime);
                            if (tsDeltaTime.TotalMilliseconds < poTimer.Interval)
                            {
                                break;
                            }
                            else
                            {
                                poTimer.m_dtLastTime = DateTime.Now;
                            }

                            poTimer.OnTick(EventArgs.Empty);
                            break;
                        }

                        Thread.Sleep(1);
                    }
                });
                _mta.IsBackground = true;
                _mta.Priority = ThreadPriority.Lowest;
                _mta.Start();
                return true;
            }
        }

        internal bool Start(Timer timer)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return false;
                }

                if (timer == null)
                {
                    return false;
                }

                if (_m.ContainsKey(timer))
                {
                    return false;
                }

                LinkedListNode<Timer> poNewNode = _s.AddLast(timer);
                if (poNewNode == null)
                {
                    return false;
                }

                _m.Add(timer, poNewNode);
                try
                {
                    return true;
                }
                finally
                {
                    this.Run();
                }
            }
        }

        internal bool Stop(Timer timer)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return false;
                }

                if (timer == null)
                {
                    return false;
                }

                if (!_m.TryGetValue(timer, out LinkedListNode<Timer> n))
                {
                    return false;
                }

                _s.Remove(n);
                _m.Remove(timer);
                _i.Remove(n);
                return true;
            }
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    _s.Clear();
                    _m.Clear();
                    _mta = null;

                    _s = null;
                    _m = null;
                    _i = null;
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
