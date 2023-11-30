namespace GVMServer.Web
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using GVMServer.Cache;
    using GVMServer.DDD.Service;
    using GVMServer.Log;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    public class Startup
    {
        private readonly RewriteRawUriQueryStringConfiguration m_RewriteRawUriQueryStringConfiguration = new RewriteRawUriQueryStringConfiguration();
        private static IConfiguration m_Configuration = null;

        private class RewriteRawUriQueryStringConfiguration
        {
            private Stopwatch m_Stopwatch = new Stopwatch();
            private bool m_Than = false;

            public static implicit operator bool(RewriteRawUriQueryStringConfiguration configuration)
            {
                if (configuration == null)
                {
                    return false;
                }
                return configuration.Is();
            }

            private bool Is()
            {
                IConfiguration configuration = m_Configuration;
                if (configuration == null)
                {
                    return false;
                }

                lock (this)
                {
                    if (!m_Stopwatch.IsRunning ||
                        m_Stopwatch.ElapsedMilliseconds >= 3000)
                    {
                        configuration = configuration.GetSection("Web");
                        try
                        {
                            m_Than = configuration.GetSection("RewriteRawUriQueryString").Get<bool>();
                        }
                        catch (Exception) { }
                        m_Stopwatch.Restart();
                    }
                    return m_Than;
                }
            }
        }

        public static IConfiguration GetDefaultConfiguration() => m_Configuration;

        public Startup(IConfiguration configuration)
        {
            m_Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc((options) =>
            {
                IConfigurationSection config = GetDefaultConfiguration().GetSection("Web");
                options.RespectBrowserAcceptHeader = config.GetValue<bool>("RespectBrowserAcceptHeader"); // 优先考虑浏览器请求头
                options.ReturnHttpNotAcceptable = config.GetValue<bool>("ReturnHttpNotAcceptable");
            }).AddJsonOptions((options) =>
            {
                IConfigurationSection config = GetDefaultConfiguration().GetSection("Json");
                if (config.GetValue<bool>("NotMinHumpFormat"))
                {
                    // 设置不使用驼峰格式处理，由后台字段确定大小写
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
                if (!config.GetValue<bool>("NullValueHandling")) // 不返回值为NULL的属性
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                }
                else
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
                }
                // 设置日期格式转换类型
                options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                if (!config.GetValue<bool>("Indented")) // 设置不允许缩进格式处理
                {
                    options.SerializerSettings.Formatting = Formatting.None;
                }
                else
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                }
                // 添加日期格式转换器的实例
                IsoDateTimeConverter converter = new IsoDateTimeConverter()
                {
                    DateTimeFormat = config.GetValue<string>("DateTimeFormat")
                };
                options.SerializerSettings.Converters.Add(converter);
            });

            services.AddApplicationInsightsTelemetry(GetDefaultConfiguration());
        }

        private ForwardedHeadersOptions GetForwardedHeadersOptions()
        {
            IConfigurationSection config = GetDefaultConfiguration().GetSection("Web");
            ForwardedHeadersOptions options = new ForwardedHeadersOptions()
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            };
            var ForwardedForKnownProxies = config.GetValue<string[]>("ForwardedForKnownProxies");
            if (ForwardedForKnownProxies != null)
            {
                foreach (string s in ForwardedForKnownProxies)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        continue;
                    }
                    options.KnownProxies.Add(IPAddress.Parse(s));
                }
            }
            return options;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders(GetForwardedHeadersOptions()).UseAuthentication(); // Microsoft.AspNetCore.HttpOverrides (Nuget) 
            app.UseRewriter();
            app.Use(middleware => async context => // 重写查询字符串URL资源
            {
                HttpRequest request = context.Request;
                if (m_RewriteRawUriQueryStringConfiguration)
                {
                    string origin = request.QueryString.Value;
                    string current = origin.RewriteQueryString();
                    if (current != origin)
                    {
                        request.QueryString = new QueryString(current); 
                    }
                }
                await middleware(context);
            });

            RedisClientManager.Create(m_Configuration.GetSection("Redis"));
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/api/Error");
            }

            IConfigurationSection config = GetDefaultConfiguration().GetSection("Web");
            if (config.GetValue<bool>("UseDirectoryBrowser"))
            {
                app.UseDirectoryBrowser();
            }

            app.UseFileServer();
            app.UseStaticFiles();
            app.UseDefaultFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "api/{controller}/{action}/{id?}",
                    defaults: new { controller = "ServerController", action = "Get" });
            });

            if (GetDefaultConfiguration().GetSection("Web").GetValue<bool>("AddConsoleAndAddDebugLogger"))
            {
                loggerFactory.AddConsole(GetDefaultConfiguration().GetSection("Logging"));
                loggerFactory.AddDebug();
            }

#pragma warning disable CS0612 // 类型或成员已过时
            app.UseApplicationInsightsRequestTelemetry();
            app.UseApplicationInsightsExceptionTelemetry();
#pragma warning restore CS0612 // 类型或成员已过时

            InitializeLogController();

            ServiceObjectContainer.Load(typeof(Program).Assembly);
            ServiceObjectContainer.Load(typeof(StatisticsController).Assembly);
        }

        private static void InitializeLogController()
        {
            LogController log = new LogController(() => m_Configuration.GetSection("Logging").GetSection("Root").Get<string>());
            ServiceObjectContainer.Register(log);
        }
    }
}
