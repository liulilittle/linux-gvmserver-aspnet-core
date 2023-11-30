namespace GVMServer.Ns.Deployment
{
    using global::System;
    using global::System.Runtime.InteropServices;

    public class PerformanceCounter
    {
        [DllImport("PDH.dll", CharSet = CharSet.Ansi)]
        private extern static int PdhOpenQuery(string szDataSource, int dwUserData, ref IntPtr phQuery);

        [DllImport("PDH.dll", CharSet = CharSet.Ansi)]
        private extern static int PdhVbAddCounter(IntPtr hQuery, string szFullCounterPath, ref IntPtr phCounter);

        [DllImport("PDH.dll", CharSet = CharSet.Ansi)]
        private extern static int PdhCollectQueryData(IntPtr hQuery);

        [DllImport("PDH.dll", SetLastError = true)]
        private extern static double PdhVbGetDoubleCounterValue(IntPtr hCounter, out int dwCounterStatus);

        [DllImport("PDH.dll", CharSet = CharSet.Ansi)]
        private extern static int PdhRemoveCounter(IntPtr hCounter);

        [DllImport("PDH.dll", CharSet = CharSet.Ansi)]
        private extern static int PdhCloseQuery(IntPtr hQuery);

        private IntPtr m_phQuery = IntPtr.Zero;
        private IntPtr m_phCounter = IntPtr.Zero;

        public PerformanceCounter(int pid, string counter)
        {
            if (0 != PdhOpenQuery(null, pid, ref m_phQuery))
            {
                throw new SystemException("The handle to the PerformanceCounter could not be opened");
            }
            PdhVbAddCounter(m_phQuery, counter, ref m_phCounter);
            if (m_phCounter == IntPtr.Zero)
            {
                PdhCloseQuery(m_phQuery);
                throw new SystemException("Unable to add a performance counter instance");
            }
        }

        private const int PDH_CSTATUS_VALID_DATA = 0;
        private const int PDH_CSTATUS_NEW_DATA = 1;

        public double Next()
        {
            lock (this)
            {
                if (m_phQuery == IntPtr.Zero)
                {
                    return 0;
                }

                // 收集性能计数查询数据
                int pdhStatus = PdhCollectQueryData(m_phQuery);

                // 查询性能计数统计的值
                double dblCounterValue = PdhVbGetDoubleCounterValue(m_phCounter, out pdhStatus);
                if (pdhStatus == PDH_CSTATUS_VALID_DATA || pdhStatus == PDH_CSTATUS_NEW_DATA)
                {
                    return dblCounterValue;
                }

                return 0;
            }
        }

        public void Close()
        {
            lock (this)
            {
                PdhRemoveCounter(m_phCounter);
                PdhCloseQuery(m_phQuery);
                m_phCounter = IntPtr.Zero;
                m_phQuery = IntPtr.Zero;
            }
        }
    }
}
