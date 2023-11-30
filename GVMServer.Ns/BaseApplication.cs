namespace GVMServer.Ns
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading;
    using GVMServer.AOP.Data;
    using GVMServer.Cache;
    using GVMServer.DDD.Events;
    using GVMServer.DDD.Hub;
    using GVMServer.DDD.Service;
    using GVMServer.DDD.Service.Component;
    using GVMServer.Linq;
    using GVMServer.Log;
    using GVMServer.Net.Web.Mvc;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Functional;
    using GVMServer.Serialization;
    using GVMServer.Threading;
    using GVMServer.Threading.Coroutines;
    using Microsoft.Extensions.Configuration;

    public enum ApplicationType : byte
    {
        ApplicationType_Namespace = 1,          // 名称节点
        ApplicationType_ComputeNode = 2,        // 计算节点
        ApplicationType_Stage = 3,              // 网关
        ApplicationType_GameServer = 4,         // 游戏服务器
        ApplicationType_CrossServer = 5,        // 跨服服务器
    }

    public abstract class BaseApplication : IServiceBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IConfiguration m_Configuration = null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private volatile bool m_disposed = false;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CancellationTokenSource m_CancellationTokenSource = null;

        public IConfiguration GetConfiguration() => m_Configuration;

        private void LoadConfigurationBuilder(string[] args)
        {
            m_Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            if (m_Configuration.GetValue<bool>("ConfiguredHotUpdates"))
            {
                m_Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddCommandLine(args)
                    .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
        }

        public ApplicationType ApplicationType { get; }

        public MvcApplication WebMvcApplication { get; private set; }

        public BaseApplication(ApplicationType applicationType)
        {
            this.ApplicationType = applicationType;
        }

        protected virtual void ProcessCommandText(string commandText)
        {

        }

        private void BackgroundListenInputCommands()
        {
            new Thread(() =>
            {
                while (!this.m_disposed)
                {
                    string commands = Console.ReadLine() ?? string.Empty;
                    commands = commands.TrimStart().TrimEnd();
                    if (!string.IsNullOrEmpty(commands))
                    {
                        ProcessCommandText(commands);
                    }
                }
            }).Start();
        }

        public virtual void Run(string[] args)
        {
            LoadConfigurationBuilder(args);
            Initialize();
            BackgroundListenInputCommands();
            WaitForCancellationTokenSource();
        }

        private void InitializeWebMvc()
        {
            MvcApplication mvc = CreateWebMvc();
            if (mvc != null)
            {
                mvc.Start(GetConfiguration().GetSection("Web").GetSection("Prefixes").Get<string[]>());
            }
            WebMvcApplication = mvc;
        }

        protected virtual MvcApplication CreateWebMvc()
        {
            MvcApplication mvc = new MvcApplication();
            mvc.Controllers.Load(typeof(BaseApplication).Assembly);
            return mvc;
        }

        protected virtual void LoadAllEvents()
        {
            EventBus.Current.Subscribe(typeof(ServiceObjectContainer).Assembly);
            EventBus.Current.Subscribe(typeof(BaseApplication).Assembly);
        }

        protected virtual void LoadAllServices()
        {
            ServiceObjectContainer.Load(typeof(ServiceObjectContainer).Assembly);
            ServiceObjectContainer.Load(typeof(BaseApplication).Assembly);
        }

        protected virtual void Initialize()
        {
            SystemEnvironment.IsWindows();
            Console.OutputEncoding = Encoding.UTF8;
            m_CancellationTokenSource = new CancellationTokenSource();

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                StopApplication();
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => StopApplication();

            InitializeEnvironment();
            UpdateConcurrentConfiguration(GetConfiguration());
            PrepareCacheAccessor();
            InitializeLogController();
            InitializeWebMvc();
            LoadAllEvents();
            LoadAllServices();
            LoadAllPlanningConfiguration();
        }

        protected virtual void PrepareCacheAccessor()
        {
            RedisClientManager.Create(m_Configuration.GetSection("Redis"));
        }

        protected virtual void LoadAllPlanningConfiguration()
        {
            ServiceObjectContainer.Get<NsPlanningConfiguration>().LoadAllFromLocalHost();
            ServiceObjectContainer.Get<NsPlanningConfiguration>().ReadAllFromLocalHost();
        }

        private void InitializeEnvironment()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            BinaryFormatter.DefaultEncoding = Encoding.GetEncoding(GetConfiguration().GetValue<string>("ExecutionCharacterSet"));
        }

        private void UpdateConcurrentConfiguration(IConfiguration configuration)
        {
            IConfiguration config = configuration.GetSection("Web").GetSection("ConcurrentConnections");

            ThreadPool.SetMinThreads(config.GetValue<int>("MinHandlingWorkerCount"), config.GetValue<int>("MinCompletionPortCount"));
            ThreadPool.SetMaxThreads(config.GetValue<int>("MaxHandlingWorkerCount"), config.GetValue<int>("MaxCompletionPortCount"));
        }

        private void InitializeLogController()
        {
            LogController log = new LogController(() => m_Configuration.GetSection("Logging").GetSection("Root").Get<string>());
            ServiceObjectContainer.Register(log);
        }

        protected virtual bool WaitForCancellationTokenSource()
        {
            try
            {
                if (m_CancellationTokenSource == null)
                {
                    return false;
                }

                if (m_CancellationTokenSource.IsCancellationRequested)
                {
                    return false;
                }

                return m_CancellationTokenSource.Token.WaitHandle.WaitOne();
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected virtual void StopApplication()
        {
            m_disposed = true;
            WebMvcApplication?.Stop();

            TaskScheduler.Exit();
            HubContainer.Dispose();
            EventBus.Current?.Dispose();
            TimerScheduler.Default?.Dispose();
            DataModelProxyConverter.GetInstance()?.Dispose();

            ServiceBaseContainer.Current.Dispose();
            ServiceFilterContainer.Current.Dispose();

            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource = null;
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            
        }
    }
}
