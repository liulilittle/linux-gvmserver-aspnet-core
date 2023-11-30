namespace GVMServer.W3Xiyou.Net.Mvh
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SocketHandlerAttribute : Attribute
    {
        /// <summary>
        /// 命令号
        /// </summary>
        public ushort CommandId { get; set; } = 0;
        /// <summary>
        /// ACK超时时间
        /// </summary>
        public int AckTimeout { get; set; } = 1000;
        /// <summary>
        /// 尝试请求重传的次数
        /// </summary>
        public int RetryAckCount { get; set; } = 0;
    }
}
