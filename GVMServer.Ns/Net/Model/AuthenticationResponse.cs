namespace GVMServer.Ns.Net.Model
{
    using System;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;

    public class AuthenticationResponse : EventArgs
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public Error Code { get; set; }
        /// <summary>
        /// 身份凭证（代表服务器节点的凭证信息）
        /// </summary>
        public Ns Credentials { get; set; }
    }
}
