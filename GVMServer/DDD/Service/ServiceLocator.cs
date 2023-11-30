namespace GVMServer.DDD.Service
{
    public class ServiceLocator : IoC<IServiceLocator>
    {
        private static ServiceLocator SERVICE_LOCATOR = new ServiceLocator();

        private ServiceLocator()
        {

        }

        public static ServiceLocator Current 
        {
            get
            {
                return SERVICE_LOCATOR;
            }
        }
    }
}
