namespace GVMServer.DDD.Service.Component
{
    using System;

    public sealed class ServiceBaseContainer : SIoC<IServiceBase>
    {
        private static ServiceBaseContainer SERVICE_CONTAINER = new ServiceBaseContainer();

        public static ServiceBaseContainer Current
        {
            get
            {
                return SERVICE_CONTAINER;
            }
        }

        private ServiceBaseContainer()
        {
            
        }

        public static bool Invalid(Type clazz)
        {
            if (clazz == null || clazz.IsValueType)
            {
                return false;
            }
            return !typeof(IServiceBase).IsAssignableFrom(clazz);
        }
    }
}
