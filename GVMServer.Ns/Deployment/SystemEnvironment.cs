namespace GVMServer.Ns.Deployment
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using GVMServer.DDD.Service;
    using GVMServer.Net;
    using GVMServer.Ns.Deployment.Os;
    using GVMServer.Ns.Functional;
    using Microsoft.Extensions.Configuration;
    using Thread = System.Threading.Thread;
    using Timer = GVMServer.Threading.Timer;

    public class SystemEnvironment
    {
        private static readonly Stopwatch _getMemoryStatusWath = new Stopwatch();
        private static MEMORY_INFO _memoryStatusInfo = new MEMORY_INFO();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MEMORY_INFO
        {
            public uint dwLength; // 当前结构体大小
            public uint dwMemoryLoad; // 当前内存使用率
            public long ullTotalPhys; // 总计物理内存大小
            public long ullAvailPhys; // 可用物理内存大小
            public long ullTotalPageFile; // 总计交换文件大小
            public long ullAvailPageFile; // 总计交换文件大小
            public long ullTotalVirtual; // 总计虚拟内存大小
            public long ullAvailVirtual; // 可用虚拟内存大小
            public long ullAvailExtendedVirtual; // 保留 这个值始终为0
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

        private unsafe static MEMORY_INFO GetMemoryStatus()
        {
            lock (_getMemoryStatusWath)
            {
                if (!_getMemoryStatusWath.IsRunning || _getMemoryStatusWath.ElapsedMilliseconds >= 500)
                {
                    _getMemoryStatusWath.Restart();
                    _memoryStatusInfo.dwLength = (uint)sizeof(MEMORY_INFO);
                    if (IsWindows())
                    {
                        GlobalMemoryStatusEx(ref _memoryStatusInfo);
                    }
                    else
                    {
                        CPULinuxLoadValue.GlobalMemoryStatus(ref _memoryStatusInfo);
                    }
                }
                return _memoryStatusInfo;
            }
        }

        private static long g_llInOutNicBytesReceived = 0;
        private static long g_llInOutNicBytesSent = 0;

        private static Timer g_tmrPerSecondTickTimer = new Timer(1000);
        private static NetworkInterface m_poInOutNetworkInterface = null;
        private static string[] g_aoHostIPAddress;
        private static ConcurrentDictionary<int, Stopwatch> m_poStopWatchTable = new ConcurrentDictionary<int, Stopwatch>();

        static SystemEnvironment()
        {
            g_tmrPerSecondTickTimer.Tick += (sender, e) => OnTick(e);
            g_tmrPerSecondTickTimer.Start();

            OnTick(EventArgs.Empty);
        }

        protected static void OnTick(EventArgs e)
        {
            // 刷新当前出入流量网卡的信息
            do
            {
                NetworkInterface poInOutNetworkInterface = QUERY_INOUT_NETWORK_INTERFACE();
                if (m_poInOutNetworkInterface == null || m_poInOutNetworkInterface.Name != poInOutNetworkInterface?.Name)
                {
                    IPInterfaceStatistics poIPInterfaceStatistics = poInOutNetworkInterface?.GetIPStatistics();
                    if (poIPInterfaceStatistics != null)
                    {
                        g_llInOutNicBytesReceived = poIPInterfaceStatistics.BytesReceived;
                        g_llInOutNicBytesSent = poIPInterfaceStatistics.BytesSent;
                    }

                    PerSecondBytesSent = 0;
                    PerSecondBytesReceived = 0;
                }
                else
                {
                    IPInterfaceStatistics poIPInterfaceStatistics = poInOutNetworkInterface.GetIPStatistics();
                    if (poIPInterfaceStatistics != null)
                    {
                        PerSecondBytesSent = poIPInterfaceStatistics.BytesReceived - g_llInOutNicBytesReceived;
                        PerSecondBytesReceived = poIPInterfaceStatistics.BytesSent - g_llInOutNicBytesSent;

                        g_llInOutNicBytesReceived = poIPInterfaceStatistics.BytesReceived;
                        g_llInOutNicBytesSent = poIPInterfaceStatistics.BytesSent;
                    }
                }
                m_poInOutNetworkInterface = poInOutNetworkInterface;
            } while (false);

            // 刷新当前主机节点的网络链路畅通的数字地址
            do
            {
                var aoHostIPAddress = SocketClient.GetActivityIPAddress().Select(i => i.ToString()).ToList();
                var application = ServiceObjectContainer.Get<BaseApplication>();
                if (application != null)
                {
                    var s = application.GetConfiguration().GetSection("HostAddresses").Get<string[]>();
                    if (s != null && s.Length > 0)
                    {
                        int index = 0;
                        foreach (string i in s)
                        {
                            if (string.IsNullOrEmpty(i))
                            {
                                continue;
                            }

                            string x = i.TrimStart().TrimEnd();
                            if (string.IsNullOrEmpty(x))
                            {
                                continue;
                            }

                            aoHostIPAddress.Insert(index++, x);
                        }
                    }
                }
                g_aoHostIPAddress = aoHostIPAddress.ToArray();
            } while (false);
            if (IsWindows())
            {
                CPUWin32LoadValue.Refresh();
            }
            else
            {
                CPULinuxLoadValue.Refresh();
            }
        }

        public unsafe static bool Equals(IPAddress x, IPAddress y)
        {
            if (x == null && y == null)
                return true;
            if (x.AddressFamily != y.AddressFamily)
                return false;

            byte[] bx = x.GetAddressBytes();
            byte[] by = y.GetAddressBytes();
            if (bx.Length != by.Length)
                return false;

            fixed (byte* pinnedX = bx)
            {
                fixed (byte* pinnedY = by)
                {
                    if (bx.Length == 4)
                        return *(uint*)pinnedX == *(uint*)pinnedY; // 32bit
                    else if (bx.Length == 8)
                        return *(ulong*)pinnedX == *(ulong*)pinnedY; // 64bit
                    else if (bx.Length == 16)
                        return *(decimal*)pinnedX == *(decimal*)pinnedY; // 128bit
                    else if (bx.Length == 2)
                        return *(ushort*)pinnedX == *(ushort*)pinnedY; // 16bit
                    else if (bx.Length == 1)
                        return *pinnedX == *pinnedY;
                    else
                    {
                        for (int i = 0; i < bx.Length; ++i)
                            if (pinnedX[i] != pinnedY[i])
                                return false;
                        return true;
                    }
                }
            }
        }

        private static NetworkInterface QUERY_INOUT_NETWORK_INTERFACE()
        {
            return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(ni =>
            {
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Ppp) // PPPOE宽带拨号
                {
                    return false;
                }

                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    return false;
                }

                foreach (var addressInfo in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addressInfo.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    if (Equals(addressInfo.Address, IPAddress.Any)
                        || Equals(addressInfo.Address, IPAddress.None)
                        || Equals(addressInfo.Address, IPAddress.Broadcast)
                        || Equals(addressInfo.Address, IPAddress.Loopback))
                    {
                        continue;
                    }

                    if (IsWindows())
                    {
                        if (addressInfo.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        public static long PerSecondBytesSent { get; set; }

        public static long PerSecondBytesReceived { get; set; }

        public static double CPULOAD
        {
            get
            {
                if (IsWindows())
                {
                    return CPUWin32LoadValue.CPULOAD;
                }
                return CPULinuxLoadValue.CPULOAD;
            }
        }

        public static long AvailablePhysicalMemory => GetMemoryStatus().ullAvailPhys;

        public static long UsedPhysicalMemory
        {
            get
            {
                MEMORY_INFO mi = GetMemoryStatus();
                return (mi.ullTotalPhys - mi.ullAvailPhys);
            }
        }

        public static bool IsWindows()
        {
            var platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32NT;
        }

        public static long TotalPhysicalMemory => GetMemoryStatus().ullTotalPhys;

        public static long PrivateWorkingSet => Environment.WorkingSet;

        public static int ProcessorCount => Environment.ProcessorCount;

        public static string[] GetHostIPAddress() => g_aoHostIPAddress;

        public static NetworkInterface GetInOutNetworkInterface() => m_poInOutNetworkInterface;

        public static int ClockSleepTime(int maxConcurrent)
        {
            int intManagedThreadId = Thread.CurrentThread.ManagedThreadId;
            lock (m_poStopWatchTable)
            {
                if (!m_poStopWatchTable.TryGetValue(intManagedThreadId, out Stopwatch poStopWatch) || poStopWatch == null)
                {
                    poStopWatch = new Stopwatch();
                    poStopWatch.Start();
                    m_poStopWatchTable[intManagedThreadId] = poStopWatch;
                }
                long llElapsedWatchTicks = poStopWatch.ElapsedTicks;
                double dblTotalMilliseconds = llElapsedWatchTicks / 10000.00;
                if (dblTotalMilliseconds < 1)
                {
                    return 0;
                }
                poStopWatch.Restart();
            }
            const double MAX_USE_LOAD = NsLoadbalancing.MAX_CPULOAD_ULTIMATE_NOT_EXCEEDING;
            if (maxConcurrent <= 0)
            {
                return 0;
            }
            double dblUseLoad = SystemEnvironment.CPULOAD;
            if (dblUseLoad < MAX_USE_LOAD) // 控制CPU利用率
            {
                return 0;
            }
            else
            {
                double dblAviLoad = unchecked(dblUseLoad - MAX_USE_LOAD);
                double dblSleepTime = unchecked(dblAviLoad / maxConcurrent);
                dblSleepTime = Math.Ceiling(dblSleepTime);
                if (dblSleepTime < 1)
                {
                    dblSleepTime = 1;
                }
                return unchecked((int)dblSleepTime);
            }
        }
    }
}
