namespace GVMServer.Web
{
    using System;
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
    using GVMServer.Net;
    using GVMServer.Threading;
    using GVMServer.Threading.Coroutines;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
    using Microsoft.Extensions.Configuration;

    public static class Program
    {
        private static void UpdateConcurrentConfiguration(IConfigurationRoot configuration)
        {
            IConfiguration config = configuration.GetSection("Web").GetSection("ConcurrentConnections");

            ThreadPool.SetMinThreads(config.GetValue<int>("MinHandlingWorkerCount"), config.GetValue<int>("MinCompletionPortCount"));
            ThreadPool.SetMaxThreads(config.GetValue<int>("MaxHandlingWorkerCount"), config.GetValue<int>("MaxCompletionPortCount"));
        }

        private static MinDataRate GetMinDataRate(IConfigurationSection section)
        {
            if (section == null)
            {
                return null;
            }
            int bytesPerSecond = section.GetValue<int>("BytesPerSecond");
            int gracePeriod = section.GetValue<int>("gracePeriod");
            if (bytesPerSecond == -1 && gracePeriod == -1)
            {
                return null;
            }
            return new MinDataRate(bytesPerSecond: bytesPerSecond, gracePeriod: TimeSpan.FromSeconds(gracePeriod));
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            IConfigurationRoot configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddCommandLine(args)
             .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: false)
             .AddEnvironmentVariables()
             .Build();

            SocketClient.Initialize();
            var builder = new WebHostBuilder().UseContentRoot(Directory.GetCurrentDirectory())
                .UseApplicationInsights();
            if (configuration.GetValue<bool>("ConfiguredHotUpdates"))
            {
                builder.ConfigureAppConfiguration((cb) => cb.AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true));
            }
            builder.UseStartup<Startup>().UseUrls(configuration.GetSection("Web").GetSection("Prefixes").Get<string[]>());

            UpdateConcurrentConfiguration(configuration);
            if (!configuration.GetSection("Web").GetSection("UseHttpSys").Get<bool>())
            {
                builder.UseKestrel(options => // https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserverlimits?view=aspnetcore-2.1
                {
                    IConfigurationSection config = configuration.GetSection("Web");
                    options.ApplicationSchedulingMode = SchedulingMode.ThreadPool;
                    options.AllowSynchronousIO = config.GetValue<bool>("AllowSynchronousIO");
                    options.UseSystemd();

                    options.Limits.MaxConcurrentUpgradedConnections = null; // NULL不受限制
                    options.Limits.MaxConcurrentConnections = null; // 最大并发指数
                    options.Limits.MaxRequestBodySize = config.GetValue<int>("MaxRequestBodySize");
                    options.Limits.Http2.MaxStreamsPerConnection = int.MaxValue;

                    // 设置最小流速
                    options.Limits.MinResponseDataRate = GetMinDataRate(config.GetSection("MinResponseDataRate"));
                    options.Limits.MinRequestBodyDataRate = GetMinDataRate(config.GetSection("MinRequestBodyDataRate"));
                });
            }
            else
            {
                builder.UseHttpSys(options => // The following options are set to default values.
                {
                    IConfigurationSection config = configuration.GetSection("Web");
                    options.Authentication.Schemes = AuthenticationSchemes.None;
                    options.AllowSynchronousIO = config.GetValue<bool>("AllowSynchronousIO");
                    options.Authentication.AllowAnonymous = true;
                    options.MaxConnections = -1;
                    options.MaxAccepts = int.MaxValue;
                    options.RequestQueueLimit = int.MaxValue;
                    options.MaxRequestBodySize = config.GetValue<int>("MaxRequestBodySize");
                });
            }

            using (var host = builder.Build())
            {
                IApplicationLifetime applicationLifetime = (IApplicationLifetime)host.Services.GetService(typeof(IApplicationLifetime));
                applicationLifetime.ApplicationStopped.Register(() => StopApplicatin(host));
                host.Run();
            }
        }

        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            string stackTrace = LogController.CaptureStackTrace(e.Exception, 1);
            var log = ServiceObjectContainer.Get<LogController>();
            if (log == null)
            {
                Console.Out.WriteLine(stackTrace);
            }
            else
            {
                log.WriteLine(LogController.ERROR, stackTrace);
            }
        }

        private static void StopApplicatin(IWebHost host)
        {
            TaskScheduler.Exit();
            HubContainer.Dispose();
            EventBus.Current?.Dispose();
            TimerScheduler.Default?.Dispose();
            DataModelProxyConverter.GetInstance()?.Dispose();

            RedisClientManager.GetDefault()?.Dispose();
            ServiceBaseContainer.Current.Dispose();
            ServiceFilterContainer.Current.Dispose();
        }
    }
}
