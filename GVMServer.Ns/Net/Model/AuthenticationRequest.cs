namespace GVMServer.Ns.Net.Model
{
    using System;

    public class AuthenticationRequest : EventArgs
    {
        /// <summary>
        /// 节点Id
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// 节点类型
        /// </summary>
        public ApplicationType ApplicationType { get; set; }
    }
}
