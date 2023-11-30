namespace GVMServer.DDD.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using GVMServer.AOP.Proxy;
    using GVMServer.DDD.Service.Component;

    public partial class ServiceObjectContainer
    {
        static ServiceObjectContainer()
        {
            Services = new ConcurrentDictionary<Type, object>();
        }

        public static IDictionary<Type, object> Services
        {
            get;
            private set;
        }

        public static void Load(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            lock (Services)
            {
                ServiceBaseContainer.Current.Load(assembly);
                ServiceFilterContainer.Current.Load(assembly);
            }
        }

        public static bool Register(IServiceBase service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            lock (Services)
            {
                return ServiceBaseContainer.Current.Register(service);
            }
        }

        public static T Get<T>() where T : class
        {
            Type type = typeof(T);
            lock (Services)
            {
                object service = null;
                if (!Services.TryGetValue(type, out service))
                {
                    service = New(type);
                    if (service == null)
                    {
                        return null;
                    }

                    Services.Add(type, service);
                }
                return (T)service;
            }
        }

        private static object New(Type type)
        {
            object[] attrs = type.GetCustomAttributes(typeof(ServiceObjectAttribute), false);
            if (attrs.Length <= 0)
            {
                return ServiceBaseContainer.Current.Get(type);
            }

            ServiceObjectAttribute attr = (ServiceObjectAttribute)attrs[0];
            if (attr.ServiceFilter == null)
            {
                throw new ArgumentException();
            }

            object filter = ServiceFilterContainer.Current.Get(attr.ServiceFilter);
            if (filter == null)
            {
                throw new ArgumentException();
            }

            return InterfaceProxy.New(type, (InvocationHandler)filter);
        }
    }
}
