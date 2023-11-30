namespace GVMServer.Ns.Model
{
    using System;
    using GVMServer.Ns.Net.Model;

    public class NodeSample
    {
        /// <summary>
        /// 节点Id
        /// </summary>
        public Guid Nodeid { get; set; }
        /// <summary>
        /// 节点类型
        /// </summary>
        public ApplicationType ApplicationType { get; set; }
        /// <summary>
        /// 采样信息
        /// </summary>
        public LinkHeartbeat Context { get; set; }
    }
}
