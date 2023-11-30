namespace GVMServer.DDD.Service
{
    using System;

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public partial class ServiceObjectAttribute : Attribute
    {
        /// <summary>
        /// 服务对象名
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// 服务过滤器
        /// </summary>
        public Type ServiceFilter { get; set; }
    }
}
