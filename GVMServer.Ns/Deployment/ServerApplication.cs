namespace GVMServer.Ns.Deployment
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading;
    using GVMServer.DDD.Service;
    using GVMServer.Linq;
    using GVMServer.Net;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Model;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Model;
    using GVMServer.Utilities;
    using GVMServer.W3Xiyou.Docking;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using Ns = global::GVMServer.Ns.Functional.Ns;

    public unsafe class ServerApplication : IServiceBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private NsTimer m_poNSLookupTtlTimer;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private NsTimer m_poNSRelookupTimer;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ISocketHandler m_poSocketHandler;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Ns m_poCredentials = null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private volatile int m_iSystemUnqiueIdRight32 = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private volatile int m_iSystemUnqiueIdCredentials32 = 0;

        public const int MaxChannelConcurrent = 4;

        public NsClient NamespaceChannel { get; private set; }

        public ApplicationChannel ApplicationChannel { get; private set; }

        public virtual Ns Credentials
        {
            get
            {
                return this.m_poCredentials;
            }
            private set
            {
                this.m_poCredentials = value;
                if (value == null)
                {
                    this.m_iSystemUnqiueIdCredentials32 = 0;
                }
                else
                {
                    Guid guid = value.AssignNodeid;
                    this.m_iSystemUnqiueIdCredentials32 = Hash32.GetHashCode((byte*)&guid, 0, sizeof(Guid));
                }
            }
        }

        public ApplicationType ApplicationType => this.Application.ApplicationType;

        public Guid Id => this.Credentials?.AssignNodeid ?? Guid.Empty;

        public BaseApplication Application { get; }

        public virtual long GenerateNewId()
        {
            Ns credentials = this.Credentials;
            if (credentials == null)
            {
                return 0;
            }
            long n = 0;
            int* p = (int*)&n;
            while (n == 0)
            {
                p[0] = m_iSystemUnqiueIdCredentials32;
                p[1] = Interlocked.Increment(ref m_iSystemUnqiueIdRight32);
            }
            return n;
        }

        public ServerApplication(BaseApplication application)
        {
            this.Application = application ?? throw new ArgumentNullException(nameof(application));
            this.ApplicationChannel = new ApplicationChannel(this, GetMaxRetransmissionConcurrent());
        }

        protected virtual int GetMaxRetransmissionConcurrent() => GetMaxRetransmissionConcurrent(GetApplicationConfiguration());

        public static int GetMaxRetransmissionConcurrent(IConfiguration configuration)
        {
            if (configuration == null)
            {
                return 0;
            }
            int iMaxRetransmissionConcurrent = configuration.
                GetSection("Gateway").
                GetSection("MaxRetransmissionConcurrent").Get<int>();
            return iMaxRetransmissionConcurrent;
        }

        protected virtual IConfiguration GetApplicationConfiguration()
        {
            return this.Application.GetConfiguration();
        }

        public virtual int MaxTimeout => 3000;

        public virtual IConfiguration GetConfiguration() => GetConfiguration(GetApplicationConfiguration());

        private void LoadCredentialsAsync(string certfile, object state)
        {
            Ns credentials = null;
            bool retrievecredentials = false;
            if (!File.Exists(certfile))
            {
                retrievecredentials = true;
            }
            else
            {
                string s = File.ReadAllText(certfile, Encoding.UTF8);
                if (!string.IsNullOrEmpty(s))
                {
                    try
                    {
                        credentials = XiYouSerializer.DeserializeJson<Ns>(s);
                    }
                    catch (Exception)
                    {

                    }
                }
                if (certfile == null)
                {
                    retrievecredentials = true;
                }
            }
            if (!retrievecredentials)
            {
                this.Credentials = credentials;
                OnLoadCredentials(EventArgs.Empty);
            }
            else
            {
                if (File.Exists(certfile))
                {
                    File.Delete(certfile);
                }
                PutonrecordAsync(callback: (e) =>
                {
                    if (e == null || e.Code != (int)Error.Error_Success || (credentials = e.Tag) == null)
                    {
                        ProcessLoadCredentialsError(e, state);
                    }
                    else
                    {
                        File.WriteAllText(certfile,
                            JObject.FromObject(credentials).ToString(),
                            Encoding.UTF8);
                        this.Credentials = credentials;
                        OnLoadCredentials(EventArgs.Empty);
                    }
                });
            }
        }

        protected virtual void ProcessLoadCredentialsError(GenericResponse<Error, Ns> e, object state)
        {
            Console.WriteLine("SystemError:\r\nThe server can't put on record(NS) from error code=" +
                (e?.Code ?? Error.Error_UnknowTheUnhandlingExceptionWarning));
            Thread.Sleep(5000);
            Environment.Exit(0);
        }

        public virtual void Start()
        {
            LinkHeartbeat.CaptureNow();
            StopLookupTtlTimer();
            StartLookupTtlTimer();
            LoadCredentialsAsync(GetFileCert(), null);
        }

        protected virtual string GetFileCert()
        {
            return (GetConfiguration().GetSection("NSFileCert").Get<string>() ?? string.Empty).TrimStart().TrimEnd();
        }

        private void StopLookupTtlTimer()
        {
            lock (this)
            {
                var poNSLookupTimer = this.m_poNSLookupTtlTimer;
                if (poNSLookupTimer != null)
                {
                    poNSLookupTimer.Stop();
                    poNSLookupTimer = null;
                }
            }
        }

        protected virtual string GetServer()
        {
            return (GetConfiguration().GetSection("NSServer").Get<string>() ?? string.Empty).TrimStart().TrimEnd();
        }

        protected virtual int GetLookupTtl()
        {
            return GetConfiguration().GetSection("NSLookupTtl").Get<int>();
        }

        private void StartLookupTtlTimer()
        {
            int iTTL = GetLookupTtl();
            bool bImmediately = false;
            lock (this)
            {
                var poNSLookupTimer = this.m_poNSLookupTtlTimer;
                if (poNSLookupTimer == null)
                {
                    bImmediately = true;
                    poNSLookupTimer = new NsTimer(iTTL * 1000);
                    poNSLookupTimer.Tick += (sender, e) => OnLookupTtlTick(e);
                    poNSLookupTimer.Start();
                    this.m_poNSLookupTtlTimer = poNSLookupTimer;
                }
            }
            if (bImmediately)
            {
                OnLookupTtlTick(EventArgs.Empty);
            }
        }

        private void CloseRelookupTimer()
        {
            lock (this)
            {
                NsTimer poNSRelookupTimer = m_poNSRelookupTimer;
                if (poNSRelookupTimer != null)
                {
                    poNSRelookupTimer.Stop();
                    poNSRelookupTimer.Dispose();
                    poNSRelookupTimer = null;
                }
            }
        }

        protected virtual ApplicationType GetEstablishChannelsApplicationType()
        {
            ApplicationType iEstablishChannelsApplicationType = unchecked((ApplicationType)(GetConfiguration().
                GetSection("EstablishChannelsApplicationType").Get<int?>() ?? 0));
            if (!Enum.IsDefined(typeof(ApplicationType), iEstablishChannelsApplicationType))
            {
                throw new ArgumentOutOfRangeException("Configuration of EstablishChannelsApplicationType overflow ApplicationType defined scope");
            }
            return iEstablishChannelsApplicationType;
        }

        protected virtual void OnLookupTtlTick(EventArgs e)
        {
            ApplicationType applicationType = GetEstablishChannelsApplicationType();
            LookupAsync(applicationType, (response) =>
            {
                if (response == null || response.Code != Error.Error_Success || response.Tag == null || response.Tag.Length <= 0)
                {
                    lock (this)
                    {
                        CloseRelookupTimer();
                        m_poNSRelookupTimer = new NsTimer(this.MaxTimeout);
                        m_poNSRelookupTimer.Tick += (x1, x2) =>
                        {
                            CloseRelookupTimer();
                            OnLookupTtlTick(e);
                        };
                        m_poNSRelookupTimer.Start();
                    }
                }
                else
                {
                    NodeSample[] samples = response.Tag;
                    if (samples == null)
                    {
                        samples = new NodeSample[0];
                    }
                    ReestablishApplicationChannel(applicationType, samples);
                }
            });
        }

        protected virtual bool EchoNodehost(IEnumerable<string> nodehosts, int port, Action<string, int> success)
        {
            return NsClient.Echo(nodehosts, port, success);
        }

        protected virtual void ReestablishApplicationChannel(ApplicationType applicationType, params NodeSample[] samples)
        {
            foreach (var sample in samples)
            {
                if (sample == null || sample.Context == null)
                {
                    continue;
                }

                var aszNodehostIPAddresses = sample.Context.NodehostIPAddresses;
                if (aszNodehostIPAddresses == null || aszNodehostIPAddresses.Length <= 0)
                {
                    continue;
                }

                var iNodehostPort = sample.Context.SocketMvhCommunicationPort;
                EchoNodehost(
                    nodehosts: aszNodehostIPAddresses,
                    port: iNodehostPort,
                    success: (nodehost, port) => ReestablishApplicationChannel(sample, $"{ nodehost }:{ port }")
                );
            }
        }

        protected virtual void ReestablishApplicationChannel(NodeSample sample, string addresses)
        {
            this.ApplicationChannel.AddChannel(sample.ApplicationType, sample.Nodeid, this.ApplicationType, this.Id, addresses);
        }

        protected virtual void OnLoadCredentials(EventArgs e)
        {
            this.NamespaceChannel = new NsClient(this.GetSocketHandler(),
                this.ApplicationType,
                this.Id,
                this.Application.ApplicationType,
                this.Credentials.AssignNodeid,
                GetServer(), MaxChannelConcurrent);
            this.NamespaceChannel.Run();
        }

        public virtual ISocketHandler GetSocketHandler()
        {
            lock (this)
            {
                if (m_poSocketHandler == null)
                {
                    m_poSocketHandler = new SocketHandler();
                }
                return m_poSocketHandler;
            }
        }

        protected virtual string GetWebHost()
        {
            return GetWebHost(this.Application.GetConfiguration());
        }

        public static IConfiguration GetConfiguration(IConfiguration configuration)
        {
            return configuration?.GetSection("NSCert");
        }

        public static string GetWebHost(IConfiguration configuration)
        {
            if (configuration == null)
            {
                return string.Empty;
            }
            string rawUri = (GetConfiguration(configuration).GetSection("NSWebServer").
                                Get<string>() ?? string.Empty).TrimStart().TrimEnd();
            if (rawUri.Length <= 0)
            {
                return string.Empty;
            }
            rawUri = rawUri.Replace('\\', '/');
            if (rawUri[rawUri.Length - 1] != '/')
            {
                rawUri += '/';
            }
            return rawUri;
        }

        public virtual void PutonrecordAsync(Action<GenericResponse<Error, Ns>> callback)
        {
            string rawUri = this.GetWebHost();
            if (string.IsNullOrEmpty(rawUri))
            {
                callback?.Invoke(null);
            }
            else
            {
                try
                {
                    EthernetInterface eth = SocketClient.
                        GetAllEthernetInterfaces((i) => i.NetworkInterface.OperationalStatus == OperationalStatus.Up).FirstOrDefault();
                    rawUri += "api/ns/putonrecord?";
                    rawUri += $"ApplicationType={(int)this.Application.ApplicationType}&AddressMask={eth.MacAddress}";
                    rawUri = new Uri(rawUri).ToString();

                    XiYouUtility.GetFromUrlAsync<GenericResponse<Error, Ns>>(rawUri, this.MaxTimeout, (error, e) => callback?.Invoke(e));
                }
                catch (Exception)
                {
                    callback.Invoke(null);
                }
            }
        }

        public virtual void LookupAsync(ApplicationType applicationType, Action<GenericResponse<Error, NodeSample[]>> callback, bool allLookup = true)
        {
            string rawUri = this.GetWebHost();
            if (string.IsNullOrEmpty(rawUri))
            {
                callback?.Invoke(null);
            }
            else
            {
                try
                {
                    if (!allLookup)
                    {
                        rawUri += "api/ns/lookup?";
                        rawUri += $"ApplicationType={(int)applicationType}";
                        rawUri = new Uri(rawUri).ToString();
                    }
                    else
                    {
                        rawUri += "api/ns/lookupall?";
                        rawUri += $"&ApplicationType={(int)applicationType}";
                        rawUri = new Uri(rawUri).ToString();
                    }

                    try
                    {
                        if (!allLookup)
                        {
                            XiYouUtility.GetFromUrlAsync<GenericResponse<Error, NodeSample>>(rawUri, this.MaxTimeout, (error, e) =>
                            {
                                if (e == null)
                                {
                                    callback(null);
                                }
                                else
                                {
                                    NodeSample[] samples = null;
                                    if (e.Tag == null)
                                    {
                                        samples = new NodeSample[0];
                                    }
                                    else
                                    {
                                        samples = new[] { e.Tag };
                                    }
                                    callback(new GenericResponse<Error, NodeSample[]>()
                                    {
                                        Code = e.Code,
                                        Message = e.Message,
                                        Tag = samples
                                    });
                                }
                            });
                        }
                        else
                        {
                            XiYouUtility.GetFromUrlAsync<GenericResponse<Error, NodeSample[]>>(rawUri, this.MaxTimeout, (error, e) => callback?.Invoke(e));
                        }
                    }
                    catch (Exception)
                    {
                        XiYouUtility.GetFromUrlAsync<GenericResponse<Error, object>>(rawUri, this.MaxTimeout, (error, e) =>
                        {
                            if (callback == null)
                            {
                                return;
                            }

                            if (e == null)
                            {
                                callback(null);
                                return;
                            }

                            try
                            {
                                object v = e.Tag;
                                if (v != null)
                                {
                                    if (v is JObject o)
                                    {
                                        v = new[] { o.ToObject<NodeSample>() };
                                    }
                                    else if (v is JArray a)
                                    {
                                        NodeSample[] sx = new NodeSample[a.Count];
                                        int index = 0;
                                        foreach (JToken t in a)
                                        {
                                            if (t.Type == JTokenType.Object)
                                            {
                                                sx[index++] = t.ToObject<NodeSample>();
                                            }
                                        }
                                        v = sx;
                                    }
                                }
                                e.Tag = v;
                            }
                            catch (Exception)
                            {
                                e.Code = Error.Error_UnableToDeserializeResponseJsonContent;
                            }

                            NodeSample[] samples = e.Tag as NodeSample[];
                            callback(new GenericResponse<Error, NodeSample[]>()
                            {
                                Code = e.Code,
                                Message = e.Message,
                                Tag = samples
                            });
                        });
                    }
                }
                catch (Exception)
                {
                    callback.Invoke(null);
                }
            }
        }
    }
}