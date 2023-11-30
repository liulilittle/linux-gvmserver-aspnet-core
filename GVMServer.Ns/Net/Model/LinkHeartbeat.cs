namespace GVMServer.Ns.Net.Model
{
    using System;
    using System.Diagnostics;
    using System.Net.NetworkInformation;
    using GVMServer.DDD.Service;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Net.Mvh;

    public class LinkHeartbeat : EventArgs
    {
        private static readonly Stopwatch m_stCaptureStopWatch = new Stopwatch();
        private static LinkHeartbeat m_poLinkHeartbeatCurrent;
        /// <summary>
        /// CPU利用率（CPU频率）
        /// </summary>
        public double CPULOAD { get; set; }
        /// <summary>
        /// 可用客户端数量
        /// </summary>
        public int AvailableClientNumber { get; set; }
        /// <summary>
        /// 每秒输出流速
        /// </summary>
        public long PerSecondBytesSent { get; set; }
        /// <summary>
        /// 每秒输入流数
        /// </summary>
        public long PerSecondBytesReceived { get; set; }
        /// <summary>
        /// 网卡最大带宽速率
        /// </summary>
        public long NicMaxBandwidthSpeeds { get; set; }
        /// <summary>
        /// 可用物理内存大小（系统用户态）
        /// </summary>
        public long AvailablePhysicalMemory { get; set; }
        /// <summary>
        /// 总物理内存大小（系统用户态）
        /// </summary>
        public long TotalPhysicalMemory { get; set; }
        /// <summary>
        /// 私有工作内存大小（进程持有）
        /// </summary>
        public long PrivateWorkingSet { get; set; }
        /// <summary>
        /// 套接字Mvh通信层端口
        /// </summary>
        public int SocketMvhCommunicationPort { get; set; }
        /// <summary>
        /// 节点主机通信地址
        /// </summary>
        public string[] NodehostIPAddresses { get; set; }
        /// <summary>
        /// 捕获之前的心跳信息实例（ago）
        /// </summary>
        /// <returns></returns>
        public static LinkHeartbeat Capture()
        {
            lock (m_stCaptureStopWatch)
            {
                double dblTotalMilliseconds = m_stCaptureStopWatch.ElapsedTicks / 10000.00;
                if (m_poLinkHeartbeatCurrent != null && dblTotalMilliseconds < 1000)
                {
                    return m_poLinkHeartbeatCurrent;
                }
                else
                {
                    m_stCaptureStopWatch.Restart();
                }
                return CaptureNow();
            }
        }
        /// <summary>
        /// 捕获现在的心跳信息实例（now）
        /// </summary>
        /// <returns></returns>
        public static LinkHeartbeat CaptureNow()
        {
            LinkHeartbeat request = new LinkHeartbeat()
            {
                CPULOAD = SystemEnvironment.CPULOAD,
                PrivateWorkingSet = SystemEnvironment.PrivateWorkingSet,
                TotalPhysicalMemory = SystemEnvironment.TotalPhysicalMemory,
                AvailablePhysicalMemory = SystemEnvironment.AvailablePhysicalMemory,
                AvailableClientNumber = 0,
                PerSecondBytesReceived = SystemEnvironment.PerSecondBytesReceived,
                PerSecondBytesSent = SystemEnvironment.PerSecondBytesSent,
                NicMaxBandwidthSpeeds = 0,
                SocketMvhCommunicationPort = 0,
                NodehostIPAddresses = SystemEnvironment.GetHostIPAddress(),
            };
            SocketMvhApplication mvh = ServiceObjectContainer.Get<SocketMvhApplication>();
            if (mvh != null)
            {
                request.SocketMvhCommunicationPort = mvh.Port;
                request.AvailableClientNumber += mvh.AvailableChannels;
            }
            NetworkInterface ni = SystemEnvironment.GetInOutNetworkInterface();
            if (ni != null)
            {
                request.NicMaxBandwidthSpeeds = ni.Speed;
            }
            return unchecked(m_poLinkHeartbeatCurrent = request);
        }
    }
}
