namespace GVMServer.DDD.Service.Component
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public sealed class ServiceFilterContainer : SIoC<ServiceFilter>
    {
        private static ServiceFilterContainer SERVICE_CONTAINER = new ServiceFilterContainer();

        public static ServiceFilterContainer Current
        {
            get
            {
                return SERVICE_CONTAINER;
            }
        }

        protected override object Resolve(Type clazz, bool alloc, params Type[] exceptive)
        {
            exceptive = new Type[] { typeof(IServiceBase) };
            return base.Resolve(clazz, alloc, exceptive);
        }

        protected override object Resolve(ConstructorInfo ctor, bool alloc, IDictionary<Type, object> exceptive)
        {
            exceptive = new Dictionary<Type, object>();
            foreach (var pi in ctor.GetParameters())
            {
                Type clazz = pi.ParameterType;
                object obj = ServiceBaseContainer.Current.Get(clazz);
                if (obj != null)
                {
                    exceptive.Add(clazz, obj);
                }
            }
            return base.Resolve(ctor, alloc, exceptive);
        }
    }
}
