namespace GVMServer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using GVMServer.AOP.Data;
    using GVMServer.DDD.Events;
    using GVMServer.DDD.Hub;
    using GVMServer.DDD.Service;
    using GVMServer.DDD.Service.Component;
    using GVMServer.Hooking;
    using GVMServer.Linq;
    using GVMServer.Log;
    using GVMServer.Net;
    using GVMServer.Net.Web.Mvc;
    using GVMServer.Serialization;
    using GVMServer.Threading;
    using GVMServer.Threading.Coroutines;
    using GVMServer.W3Xiyou;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Docking.Api.Request;
    using GVMServer.W3Xiyou.Net.Mvh;
    using Microsoft.Extensions.Configuration;

    public static class MainApplication
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IConfiguration m_Configuration = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static CancellationTokenSource m_CancellationTokenSource = null;

        public static SocketMvhApplication SocketMvhApplication { get; private set; }

        public static MvcApplication WebMvcApplication { get; private set; }

        public static IConfiguration GetDefaultConfiguration() => m_Configuration;

        private static void InitializeEnvironment()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            BinaryFormatter.DefaultEncoding = Encoding.GetEncoding(GetDefaultConfiguration().GetValue<string>("ExecutionCharacterSet"));
        }

        private static void UpdateConcurrentConfiguration(IConfiguration configuration)
        {
            IConfiguration config = configuration.GetSection("Web").GetSection("ConcurrentConnections");

            ThreadPool.SetMinThreads(config.GetValue<int>("MinHandlingWorkerCount"), config.GetValue<int>("MinCompletionPortCount"));
            ThreadPool.SetMaxThreads(config.GetValue<int>("MaxHandlingWorkerCount"), config.GetValue<int>("MaxCompletionPortCount"));
        }

        private static void InitializeLogController()
        {
            LogController log = new LogController(() => m_Configuration.GetSection("Logging").GetSection("Root").Get<string>());
            ServiceObjectContainer.Register(log);
        }

        private static bool WaitForCancellationTokenSource()
        {
            try
            {
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

        private static void LoadConfigurationBuilder(string[] args)
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

        [STAThread]
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            m_CancellationTokenSource = new CancellationTokenSource();

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            SocketClient.Initialize();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                StopApplication();
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => StopApplication();

            LoadConfigurationBuilder(args);
            InitializeEnvironment();
            UpdateConcurrentConfiguration(GetDefaultConfiguration());
            InitializeLogController();
            MeasureProtoCppCode();

            XiYouSdkConfiguration.LoadFrom(GetDefaultConfiguration());
            ServiceObjectContainer.Load(typeof(MainApplication).Assembly);

            WebMvcApplication = new MvcApplication();
            WebMvcApplication.AddHandler(new GMWebHandler());
            WebMvcApplication.Controllers.Load(typeof(MainApplication).Assembly);
            WebMvcApplication.Start(GetDefaultConfiguration().GetSection("Web").GetSection("Prefixes").Get<string[]>());

            SocketMvhApplication = GMWebHandler.CreateMvhApplication(GetDefaultConfiguration().GetSection("Gateway").GetValue<int>("ListenPort"));
            SocketMvhApplication.Handlers.Load(typeof(MainApplication).Assembly);
            SocketMvhApplication.Start();

            WaitForCancellationTokenSource();
        }

        private static void MeasureProtoCppCode()
        {
            Debug.WriteLine(LoginAccountRequest.MeasureCode());
            Debug.WriteLine(CreateOrderRequest.MeasureCode());
        }

        private static void StopApplication()
        {
            WebMvcApplication.Stop();
            SocketMvhApplication.Stop();

            TaskScheduler.Exit();
            HubContainer.Dispose();
            EventBus.Current?.Dispose();
            TimerScheduler.Default?.Dispose();
            DataModelProxyConverter.GetInstance()?.Dispose();

            ServiceBaseContainer.Current.Dispose();
            ServiceFilterContainer.Current.Dispose();

            m_CancellationTokenSource?.Cancel();
        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            string stackTrace = LogController.CaptureStackTrace(e.Exception, 1);
            var log = ServiceObjectContainer.Get<LogController>();
            if (log == null)
            {
                Console.WriteLine(stackTrace);
            }
            else
            {
                log.WriteLine(LogController.ERROR, stackTrace);
            }
        }
    }
}
