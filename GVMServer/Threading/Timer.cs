namespace GVMServer.Threading
{
    using System;

    public class Timer : IDisposable
    {
        public event EventHandler Tick;

        public TimerScheduler Scheduler { get; }

        public Timer(int interval, TimerScheduler scheduler) : this(scheduler)
        {
            this.Interval = interval;
        }

        public Timer(int interval) : this(interval, TimerScheduler.Default)
        {

        }

        public Timer() : this(TimerScheduler.Default)
        {

        }

        public Timer(TimerScheduler scheduler)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }
            this.Scheduler = scheduler;
        }

        ~Timer()
        {
            this.Dispose();
        }

        public int Interval
        {
            get
            {
                return m_iInterval;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                lock (this)
                {
                    int original = value;
                    m_iInterval = value;
                    if (original != value)
                    {
                        this.Enabled = (value > 0);
                    }
                }
            }
        }

        private void CheckAndThrowObjectDisposeException(bool staring)
        {
            bool disposing = false;
            lock (this)
            {
                disposing = this.m_disposed;
            }
            if (disposing && staring)
            {
                throw new ObjectDisposedException("Managed and unmanaged resources held by instances of the current timer have been released");
            }
        }

        public virtual bool Enabled
        {
            get
            {
                lock (this)
                {
                    return this.m_bEnabled;
                }
            }
            set
            {
                CheckAndThrowObjectDisposeException(value);
                lock (this)
                {
                    bool bOriginal = this.m_bEnabled;
                    this.m_bEnabled = value;

                    if (bOriginal != value)
                    {
                        if (value)
                        {
                            m_dtLastTime = DateTime.Now;
                            Scheduler.Start(this);
                        }
                        else
                        {
                            m_dtLastTime = DateTime.Now;
                            Scheduler.Stop(this);
                        }
                    }
                }
            }
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!this.m_disposed)
                {
                    this.Stop();
                    this.Tick = null;
                    this.m_disposed = true;
                }
            }

            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Start()
        {
            this.Enabled = true;
        }

        public void Stop()
        {
            this.Enabled = false;
        }

        protected internal virtual void OnTick(EventArgs e)
        {
            this.Tick?.Invoke(this, e);
        }

        internal DateTime m_dtLastTime;
        private bool m_disposed;
        private bool m_bEnabled;
        private int m_iInterval;
    }
}
