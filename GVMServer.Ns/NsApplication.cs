namespace GVMServer.Ns
{
    using System;
    using GVMServer.DDD.Service;
    using GVMServer.Linq;
    using GVMServer.Net;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Mvh;
    using Microsoft.Extensions.Configuration;

    public class NsApplication : BaseApplication, IServiceBase
    {
        private SocketMvhApplication m_oMvhApplication = null;

        public NsApplication() : base(ApplicationType.ApplicationType_Namespace)
        {
            
        }

        [STAThread]
        private static void Main(string[] args)
        {
            Console.Title = $"NS@{SocketClient.GetActivityIPAddress().FirstOrDefault()}";
            NsApplication application = new NsApplication();
            ServiceObjectContainer.Register(application);
            application.Run(args);
        }

        protected override void LoadAllServices()
        {
            base.LoadAllServices();
            ServiceObjectContainer.Load(typeof(NsApplication).Assembly);
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeMvhApplication();
        }

        protected virtual void InitializeMvhApplication()
        {
            int iMaxRetransmissionConcurrent = ServerApplication.GetMaxRetransmissionConcurrent(GetConfiguration());
            int iListenPort = GetConfiguration().
                GetSection("Gateway").
                GetSection("ListenPort").Get<int>();
            this.m_oMvhApplication = new SocketMvhApplication(handler: this.GetSocketHandler(),
                    port: iListenPort,
                    maxRetransmissionConcurrent: iMaxRetransmissionConcurrent);
            this.m_oMvhApplication.HandlerContainer.Load(typeof(BaseApplication).Assembly);
            ServiceObjectContainer.Register(this.m_oMvhApplication);
            this.m_oMvhApplication.Start();
        }

        protected virtual ISocketHandler GetSocketHandler()
        {
            return new SocketHandler();
        }

        protected override void StopApplication()
        {
            this.m_oMvhApplication?.Stop();
            base.StopApplication();
        }
    }
}
