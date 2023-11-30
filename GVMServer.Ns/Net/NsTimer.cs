namespace GVMServer.Ns.Net
{
    using System;
    using GVMServer.Ns.Deployment;
    using GVMServer.Threading;
    using Interlocked = System.Threading.Interlocked;

    public class NsTimer : Timer
    {
        private static TimerScheduler[] m_aoScheduler;
        private static int m_idxScheduler = 0;

        public const int MaxConcurrent = 4;

        static NsTimer()
        {
            m_aoScheduler = new TimerScheduler[Math.Min(MaxConcurrent, SystemEnvironment.ProcessorCount)];
            for (int i = 0; i < m_aoScheduler.Length; i++)
            {
                m_aoScheduler[i] = new TimerScheduler();
            }
        }

        public static TimerScheduler GetScheduler()
        {
            uint index = unchecked((uint)(Interlocked.Increment(ref m_idxScheduler) % 
                m_aoScheduler.Length));
            return m_aoScheduler[index];
        }

        public NsTimer() : base(GetScheduler())
        {

        }

        public NsTimer(int interval) : base(interval, GetScheduler())
        {

        }
    }
}
