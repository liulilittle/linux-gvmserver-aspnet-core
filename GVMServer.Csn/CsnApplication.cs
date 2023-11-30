namespace GVMServer.Csn
{
    using System;
    using GVMServer.DDD.Events;
    using GVMServer.DDD.Service;
    using GVMServer.Linq;
    using GVMServer.Net;
    using GVMServer.Net.Web.Mvc;
    using GVMServer.Ns;
    using GVMServer.Ns.Deployment;

    public class CsnApplication : BaseApplication, IServiceBase
    {
        public CsnApplication() : base(ApplicationType.ApplicationType_ComputeNode)
        {

        }

        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = $"CSN@{SocketClient.GetActivityIPAddress().FirstOrDefault()}";
            CsnApplication application = new CsnApplication();
            ServiceObjectContainer.Register(application);
            application.Run(args);
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeServerApplication();
            this.InitializeAssertApplication();
        }

        protected override void LoadAllEvents()
        {
            base.LoadAllEvents();
            EventBus.Current.Subscribe(typeof(CsnApplication).Assembly);
        }

        protected override void LoadAllServices()
        {
            base.LoadAllServices();
            ServiceObjectContainer.Load(typeof(CsnApplication).Assembly);
        }

        protected virtual void InitializeAssertApplication()
        {

        }

        protected virtual void InitializeServerApplication()
        {
            ServerApplication poApplication = ServiceObjectContainer.Get<ServerApplication>();
            poApplication.ApplicationChannel.HandlerContainer.Load(typeof(CsnApplication).Assembly);
            poApplication.Start();
        }

        protected override MvcApplication CreateWebMvc()
        {
            return null;
        }

        protected override bool WaitForCancellationTokenSource()
        {
            return base.WaitForCancellationTokenSource();
        }
    }
}
