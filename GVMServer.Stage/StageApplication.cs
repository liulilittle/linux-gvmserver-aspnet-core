namespace GVMServer.Stage
{
    using System;
    using GVMServer.DDD.Service;
    using GVMServer.Linq;
    using GVMServer.Net;
    using GVMServer.Net.Web.Mvc;
    using GVMServer.Ns;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Net.Handler;
    using Microsoft.Extensions.Configuration;

    public class StageApplication : BaseApplication, IServiceBase
    {
        private StageSocketMvhApplication m_oMvhApplication = null;

        public StageApplication() : base(ApplicationType.ApplicationType_Stage)
        {

        }

        [STAThread]
        private static void Main(string[] args)
        {
            Console.Title = $"STAGE@{SocketClient.GetActivityIPAddress().FirstOrDefault()}";
            StageApplication application = new StageApplication();
            ServiceObjectContainer.Register(application);
            ServiceObjectContainer.Register(application);
            application.Run(args);
        }

        protected virtual void InitializeMvhApplication()
        {
            int iMaxRetransmissionConcurrent = ServerApplication.GetMaxRetransmissionConcurrent(GetConfiguration());
            int iListenPort = GetConfiguration().
                GetSection("Gateway").
                GetSection("ListenPort").Get<int>();
            this.m_oMvhApplication = new StageSocketMvhApplication(handler: this.GetSocketHandler(),
                    port: iListenPort,
                    maxRetransmissionConcurrent: iMaxRetransmissionConcurrent);
            this.m_oMvhApplication.HandlerContainer.Load(typeof(BaseApplication).Assembly);
            this.m_oMvhApplication.HandlerContainer.Load(typeof(StageApplication).Assembly);

            ServiceObjectContainer.Register(this.m_oMvhApplication);
            this.m_oMvhApplication.Start();
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeMvhApplication();
            ServiceObjectContainer.Get<ServerApplication>().Start();
        }

        protected virtual ISocketHandler GetSocketHandler()
        {
            return new StageSocketHandler();
        }

        protected override void StopApplication()
        {
            this.m_oMvhApplication?.Stop();
            base.StopApplication();
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
