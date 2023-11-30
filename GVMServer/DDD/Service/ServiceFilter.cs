namespace GVMServer.DDD.Service
{
    using GVMServer.AOP.Proxy;
    using System.Reflection;
    using System;
    
    /// <summary>
    /// 服务过滤器
    /// </summary>
    public abstract class ServiceFilter : InvocationHandler
    {
        /// <summary>
        /// 服务基类
        /// </summary>
        public IServiceBase ServiceBase
        {
            get;
            private set;
        }

        /// <summary>
        /// 实例化服务过滤器
        /// </summary>
        /// <param name="service">服务基类</param>
        public ServiceFilter(IServiceBase service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }
            this.ServiceBase = service;
        }

        object InvocationHandler.InvokeMember(object obj, int rid, string name, params object[] args)
        {
            MethodBase met = InterfaceProxy.GetMethod(obj, rid);
            return this.InvokeMember(this.ServiceBase, met, args);
        }
        /// <summary>
        /// 调用成员
        /// </summary>
        /// <param name="service">被调用的伪对象</param>
        /// <param name="method">被调用的方法</param>
        /// <param name="args">调用方法传入的参数</param>
        /// <returns></returns>
        protected abstract object InvokeMember(IServiceBase service, MethodBase method, params object[] args);
    }
}
