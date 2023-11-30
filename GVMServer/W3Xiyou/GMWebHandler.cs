namespace GVMServer.W3Xiyou
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using GVMServer.Cryptography.Final;
    using GVMServer.Linq;
    using GVMServer.Net;
    using GVMServer.Net.Web;
    using GVMServer.Serialization.Ssx;
    using GVMServer.Utilities;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using Enumerable = System.Linq.Enumerable;
    using IEnumerable_Box = System.Collections.IEnumerable;

    public class GMWebHandler : IHttpHandler
    {
        public const string GSGMIL_SEGMENTED_SYMBOL = "&";

        private class GMWebInstruction
        {
            public string Interface { get; set; }

            public string Platform { get; set; }

            public uint TimeSpan { get; set; }

            public string Request { get; set; }
        };

        private class GMWebSocketRequestInvoker : SocketRequestInvoker
        {
            public GMWebSocketRequestInvoker(GMWebSocketMvhApplication mvh) : base(mvh)
            {

            }

            protected internal override void DoEvents(SocketMessage e)
            {
                base.DoEvents(e);
            }

            public override bool InvokeAsync<T>(string platform, long identifier, ushort commandId, byte[] buffer, int offset, int length, Action<SocketReuqestInvokerError, T> callback)
            {
                if (MainApplication.SocketMvhApplication is GMWebSocketMvhApplication)
                {
                    GMWebSocketMvhApplication mvh = MainApplication.SocketMvhApplication as GMWebSocketMvhApplication;
                    if (!mvh.IsAvailableIdentifier(identifier))
                    {
                        return false;
                    }
                }
                return base.InvokeAsync(platform, identifier, commandId, buffer, offset, length, callback);
            }
        }

        private class GMWebSocketMvhApplication : SocketMvhApplication
        {
            private class PersistentConnection
            {
                public DateTime OpenTime;
                public DateTime CloseTime;
            };

            private readonly ConcurrentDictionary<long, PersistentConnection> persistents = new ConcurrentDictionary<long, PersistentConnection>();
            private readonly object synobj = new object();

            public GMWebSocketMvhApplication(int port) : base(port)
            {

            }

            protected override SocketRequestInvoker CreateInvoker()
            {
                return new GMWebSocketRequestInvoker(this);
            }

            protected override void OnOpen(ISocketClient e)
            {
                base.OnOpen(e);

                long identifier = this.GetIdentifier(e);
                if (0 == identifier)
                {
                    return;
                }
                lock (this.synobj)
                {
                    if (!this.persistents.ContainsKey(identifier))
                    {
                        this.persistents.TryAdd(identifier, new PersistentConnection());
                    }
                    PersistentConnection persistent = this.persistents[identifier];
                    persistent.OpenTime = DateTime.Now;
                    persistent.CloseTime = DateTime.MinValue;
                }
            }

            protected override void OnClose(ISocketClient e)
            {
                base.OnClose(e);

                long identifier = this.GetIdentifier(e);
                if (0 == identifier)
                {
                    return;
                }
                lock (this.synobj)
                {
                    this.persistents.TryGetValue(identifier, out PersistentConnection persistent);
                    if (persistent != null)
                    {
                        persistent.CloseTime = DateTime.Now;
                    }
                }
            }

            protected override void DoEvents()
            {
                base.DoEvents();

                int InterruptTimeAckAvailableInNSeconds = this.GetInterruptTimeAckAvailableInNSeconds();
                foreach (var kv in this.persistents)
                {
                    PersistentConnection persistent = kv.Value;
                    if (null == persistent)
                    {
                        continue;
                    }
                    if (!this.IsAvailableIdentifier(kv.Key, InterruptTimeAckAvailableInNSeconds))
                    {
                        this.persistents.TryRemove(kv.Key, out persistent);
                    }
                }
            }

            protected virtual int GetInterruptTimeAckAvailableInNSeconds() => GetConfiguration().GetValue<int>("InterruptTimeAckAvailableInNSeconds");

            public virtual bool IsAvailableIdentifier(long identifier)
            {
                return this.IsAvailableIdentifier(identifier, this.GetInterruptTimeAckAvailableInNSeconds());
            }

            public virtual bool IsAvailableIdentifier(long identifier, int InterruptTimeAckAvailableInNSeconds)
            {
                PersistentConnection persistent = null;
                if (this.persistents.TryGetValue(identifier, out persistent))
                {
                    if (DateTime.MinValue == persistent.CloseTime)
                    {
                        return true;
                    }
                    if ((DateTime.Now - persistent.CloseTime).TotalSeconds < InterruptTimeAckAvailableInNSeconds)
                    {
                        return true;
                    }
                }
                return false;
            }

            public virtual IEnumerable<long> GetAllAvailableIdentifier() => this.persistents.Keys;
        };

        public static SocketMvhApplication CreateMvhApplication(int port)
        {
            return new GMWebSocketMvhApplication(port);
        }

        public static string GetCppFormatterText()
        {
            return CppStaticBinaryFormatter.CreateFormatterText(typeof(GMWebInstruction));
        }

        private static Encoding GetDefaultEncoding()
        {
            return Encoding.UTF8;
        }

        private static unsafe string EncodingToMxBuffer(Stream stream)
        {
            if (stream == null || stream.Length <= 0)
            {
                return string.Empty;
            }
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            int datalen = Convert.ToInt32(stream.Length - stream.Position);
            StringBuilder mx = new StringBuilder(datalen * 2);
            for (int i = 0; i < datalen; i++)
            {
                mx.Append(stream.ReadByte().ToString("X2"));
            }
            return mx.ToString();
        }

        private static unsafe string EncodingToMxBuffer(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            byte[] buffer = GetDefaultEncoding().GetBytes(s);
            StringBuilder mx = new StringBuilder(buffer.Length * 2);
            fixed (byte* p = buffer)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    mx.Append(p[i].ToString("X2"));
                }
            }
            return mx.ToString();
        }

        private static bool MeasureIsInvalidCodeChar(char chr)
        {
            return !((chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z') || (chr >= '0' && chr <= '9') || chr == '_');
        }

        private static string NameValueCollectionToGsGmILProtocol(IEnumerable_Box s, GMInterface interfaces = null)
        {
            try
            {
                string protocol = string.Empty;
                if (s == null || s.IsNullOrEmpty())
                {
                    return protocol;
                }
                if (interfaces.Ignore && interfaces.Request.IsNullOrEmpty())
                {
                    return protocol;
                }
                int handlingcount = 0;
                foreach (object i in s)
                {
                    if (interfaces.Ignore && (interfaces.Request == null || handlingcount >= interfaces.Request.Count))
                    {
                        break;
                    }
                    if (i == null)
                    {
                        continue;
                    }
                    string key = i as string;
                    if (i is string)
                    {
                        if (string.IsNullOrEmpty(key))
                        {
                            continue;
                        }
                    }
                    else if (i is JProperty)
                    {
                        key = (i as JProperty).Name;
                    }
                    else if (i is JToken)
                    {
                        var j = (s as JArray).IndexOf(i as JToken);
                        if (j < 0)
                        {
                            continue;
                        }
                        key = j.ToString();
                    }
                    else
                    {
                        continue;
                    }
                    string k = key.TrimStart().TrimEnd();
                    bool error = false;
                    k.FirstOrDefault(chr =>
                    {
                        if (MeasureIsInvalidCodeChar(chr))
                        {
                            error = true;
                            return true;
                        }
                        return false;
                    });
                    if (error)
                    {
                        continue;
                    }
                    else if (interfaces.Ignore)
                    {
                        var request = interfaces.Request;
                        if (request == null || !request.ContainsKey(k))
                        {
                            continue;
                        }
                        handlingcount++;
                    }
                    NameValueCollection nvc = s as NameValueCollection;
                    HttpFileCollection hfc = s as HttpFileCollection;
                    JProperty jp = i as JProperty;
                    JToken jt = i as JToken;
                    if (nvc != null)
                    {
                        protocol += $"{k}={EncodingToMxBuffer(nvc.Get(key))}" + GSGMIL_SEGMENTED_SYMBOL;
                    }
                    else if (hfc != null)
                    {
                        HttpPostedFile hpf = hfc[key];
                        if (hpf == null)
                        {
                            continue;
                        }
                        protocol += $"{k}={EncodingToMxBuffer(hpf.InputStream)}" + GSGMIL_SEGMENTED_SYMBOL;
                    }
                    else if (jp != null)
                    {
                        string v = string.Empty;
                        if (jp.HasValues)
                        {
                            v = jp.Value.ToString();
                        }
                        protocol += $"{k}={EncodingToMxBuffer(v)}" + GSGMIL_SEGMENTED_SYMBOL;
                    }
                    else if (jt != null)
                    {
                        string v = string.Empty;
                        if (jt.Type != JTokenType.Null && jt.Type != JTokenType.Undefined)
                        {
                            v = jt.ToString();
                        }
                        protocol += $"{k}={EncodingToMxBuffer(v)}" + GSGMIL_SEGMENTED_SYMBOL;
                    }
                }
                if (protocol.Length >= GSGMIL_SEGMENTED_SYMBOL.Length)
                {
                    protocol = protocol.Remove(protocol.Length - GSGMIL_SEGMENTED_SYMBOL.Length);
                }
                return protocol;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static bool CheckHttpMethod(HttpContext context, string method)
        {
            if (context == null)
            {
                return false;
            }
            else
            {
                string m1 = (context.Request.HttpMethod ?? string.Empty).Trim().ToUpper();
                string m2 = (method ?? string.Empty).Trim().ToUpper();
                if (m1 == m2)
                {
                    return true;
                }
                if (string.IsNullOrEmpty(m2))
                {
                    return true;
                }
            }
            return false;
        }

        private static string FetchRequestToGsGmILProtocol(HttpContext context)
        {
            try
            {
                string request = string.Empty;
                string s = string.Empty;
                if ((context.Request.HttpMethod ?? string.Empty).Trim().ToUpper() == "POST")
                {
                    s = NameValueCollectionToGsGmILProtocol(context.Request.Form);
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (request.Length > 0)
                        {
                            request += GSGMIL_SEGMENTED_SYMBOL + s;
                        }
                        else
                        {
                            request += s;
                        }
                    }
                    s = NameValueCollectionToGsGmILProtocol(context.Request.Files);
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (request.Length > 0)
                        {
                            request += GSGMIL_SEGMENTED_SYMBOL + s;
                        }
                        else
                        {
                            request += s;
                        }
                    }
                    context.Request.Files.Clear();
                }
                else
                {
                    s = NameValueCollectionToGsGmILProtocol(context.Request.QueryString);
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (request.Length > 0)
                        {
                            request += GSGMIL_SEGMENTED_SYMBOL + s;
                        }
                        else
                        {
                            request += s;
                        }
                    }
                }
                return request;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string FetchRequestToGsGmILProtocol(GMRequestMessage message, GMInterface interfaces = null)
        {
            if (message == null)
            {
                return string.Empty;
            }
            IEnumerable_Box o = message.parameter as IEnumerable_Box;
            if (o == null || o is string)
            {
                return string.Empty;
            }
            return NameValueCollectionToGsGmILProtocol(o, interfaces);
        }

        public static IConfigurationSection GetConfiguration()
        {
            return MainApplication.GetDefaultConfiguration().GetSection("GMControl");
        }

        private class GMInterface
        {
            public string Name { get; set; }

            public string Method { get; set; }

            public bool Ignore { get; set; }

            public class Property
            {
                public string Type { get; set; }

                public int Size { get; set; }

                public bool Nullable { get; set; }
            }

            public IDictionary<string, Property> Request { get; set; }
        }

        private static GMInterface GetInterfaces(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                path = path.TrimStart().TrimEnd();
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                else
                {
                    path = path.ToLower();
                }
                var configuration = GetConfiguration().GetSection("Interfaces");
                if (configuration == null)
                {
                    return null;
                }
                var interfaces = configuration.GetSection(path);
                if (interfaces == null)
                {
                    return null;
                }
                return interfaces.Get<GMInterface>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool CheckIsInvokeGmWebApi(HttpContext context)
        {
            try
            {
                if (context == null)
                {
                    return false;
                }
                string rawUri = context.Request.Path ?? string.Empty;
                if (string.IsNullOrEmpty(rawUri))
                {
                    return false;
                }
                rawUri = rawUri.Trim();
                while (rawUri.Length > 0 && rawUri[rawUri.Length - 1] == '/')
                {
                    rawUri = rawUri.Remove(rawUri.Length - 1);
                }
                if (string.IsNullOrEmpty(rawUri))
                {
                    return false;
                }
                rawUri = rawUri.ToLower();
                string xUri = GetConfiguration().GetValue<string>("RawUri") ?? string.Empty;
                if (xUri.Trim().ToLower() == rawUri)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private class GMRequestMessage
        {
            public int sids = 0;
            public string platform = null;
            public string action = null;
            public object parameter = null;
            public uint timestamp = 0;
            [SignIgnore]
            public string signature = null;

            public bool Fill()
            {
                string parameter = this.parameter as string;
                if (string.IsNullOrEmpty(parameter))
                {
                    this.parameter = string.Empty;
                    return true;
                }
                parameter = (parameter ?? string.Empty).TrimStart().TrimEnd();
                if (string.IsNullOrEmpty(parameter))
                {
                    this.parameter = string.Empty;
                    return true;
                }
                try
                {
                    this.parameter = XiYouSerializer.DeserializeJson<JObject>(parameter);
                }
                catch (Exception)
                {
                    try
                    {
                        this.parameter = XiYouSerializer.DeserializeJson<JArray>(parameter);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return true;
            }

            public static GMRequestMessage GetMessage(HttpContext context)
            {
                NameValueCollection nvc = null;
                if (CheckHttpMethod(context, "POST"))
                {
                    nvc = context.Request.Form;
                }
                else
                {
                    nvc = context.Request.QueryString;
                }
                GMRequestMessage message = null;
                try
                {
                    message = XiYouSerializer.DeserializeObject<GMRequestMessage>(nvc);
                }
                catch (Exception)
                {
                    return null;
                }
                if (message == null)
                {
                    return null;
                }
                message.signature = message.signature ?? string.Empty;
                if (string.IsNullOrEmpty(message.platform) ||
                    string.IsNullOrEmpty(message.action))
                {
                    return null;
                }
                return message;
            }
        }

        private static bool CheckMessageIsTimeout(GMRequestMessage message)
        {
            if (message == null)
            {
                return true;
            }
            if (GetConfiguration().GetValue<bool>("IgnoreTimeoutVerification"))
            {
                return false;
            }
            uint ts = XiYouUtility.ToTimeSpan10(DateTime.Now);
            uint df = ts - message.timestamp;
            int tep = GetConfiguration().GetValue<int>("TimestampErrorPrecision");
            if ((df & 1 << 31) != 0)
            {
                if (unchecked((int)df) >= -tep)
                {
                    return false;
                }
            }
            else if (df <= tep)
            {
                return false;
            }
            return false;
        }

        private static bool CheckMessageIsInvalidSignature(GMRequestMessage message)
        {
            try
            {
                if (message == null)
                {
                    return true;
                }

                if (GetConfiguration().GetValue<bool>("IgnoreSignatureVerification"))
                {
                    return false;
                }

                IDictionary<string, string> pairs = new Dictionary<string, string>();
                foreach (FieldInfo fi in message.GetType().GetFields())
                {
                    if (pairs.ContainsKey(fi.Name))
                    {
                        continue;
                    }
                    if (Attributes.GetAttribute<SignIgnoreAttribute>(fi) != null)
                    {
                        continue;
                    }
                    string value = (fi.GetValue(message) ?? string.Empty).ToString();
                    value = value.Trim();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }
                    pairs.Add(fi.Name, value);
                }
                string signstr = string.Empty;
                if (pairs.Count <= 0)
                {
                    return false;
                }
                else
                {
                    Enumerable.OrderBy(pairs, i => i.Key, StringComparer.Ordinal).FirstOrDefault(kv =>
                    {
                        signstr += $"{kv.Key}={kv.Value}&";
                        return false;
                    });
                }
                if (signstr.Length > 0)
                {
                    signstr = signstr.Remove(signstr.Length - 1);
                }
                if (!string.IsNullOrEmpty(signstr))
                {
                    IConfigurationSection conf = GetConfiguration();
                    string signkey = conf.GetValue<string>("GMSercrtKey");
                    if (string.IsNullOrEmpty(signkey))
                    {
                        signkey = conf.GetValue<string>("AppSercrt");
                    }
                    signstr = MD5.ToMD5String(signstr + signkey, Encoding.UTF8);
                    if (!signstr.EqualsAndIgnoreCast(message.signature))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool CheckMessageIsLimitingError(GMRequestMessage message, GMInterface interfaces, ref string tips)
        {
            try
            {
                if (message == null || interfaces == null)
                {
                    tips = "request message is null";
                    return true;
                }
                if (message.parameter == null || string.Empty.Equals(message.parameter))
                {
                    if (interfaces.Request.IsNullOrEmpty())
                    {
                        tips = "request parameter is null";
                        return true;
                    }
                    return false;
                }
                if (interfaces.Request.IsNullOrEmpty())
                {
                    return false;
                }
                Func<object, string> g2s = (o) =>
                {
                    if (o == null)
                    {
                        return null;
                    }
                    if (o is JValue)
                    {
                        JValue v = o as JValue;
                        if (v.Type == JTokenType.Null || v.Type == JTokenType.Undefined)
                        {
                            return null;
                        }
                        return v.ToString();
                    }
                    return o.ToString();
                };
                Func<string, string> gjv = (key) =>
                {
                    JObject o = message.parameter as JObject;
                    if (o != null)
                    {
                        if (!o.ContainsKey(key))
                        {
                            return null;
                        }
                        return g2s(o[key]);
                    }
                    JArray a = message.parameter as JArray;
                    if (a != null)
                    {
                        if (!int.TryParse(key, out int i) || i < 0 || i >= a.Count)
                        {
                            return null;
                        }
                        return g2s(a[i]);
                    }
                    return null;
                };
                foreach (var kv in interfaces.Request)
                {
                    var property = kv.Value;
                    if (property == null)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(kv.Key))
                    {
                        continue;
                    }
                    if (property.Nullable)
                    {
                        continue;
                    }
                    string value = gjv(kv.Key);
                    if (string.IsNullOrEmpty(value))
                    {
                        tips = "no parameter:" + kv.Key;
                        return true;
                    }
                    if (value.Length > property.Size)
                    {
                        tips = "parameter:" + kv.Key + " size:" + property.Size + " is overflow, current size is " + value.Length;
                        return true;
                    }
                    else if (property.Type == "string")
                    {
                        continue;
                    }
                    else if (property.Type == "number")
                    {
                        if (!double.TryParse(value, NumberStyles.Number | NumberStyles.Float, null, out double n)) continue;
                    }
                    else if (property.Type == "int")
                    {
                        if (!long.TryParse(value, out long n)) continue;
                    }
                    else if (property.Type == "uint")
                    {
                        if (!ulong.TryParse(value, out ulong n)) continue;
                    }
                    else if (property.Type == "guid")
                    {
                        if (!Guid.TryParse(value, out Guid n)) continue;
                    }
                    else if (property.Type == "ip")
                    {
                        if (!IPAddress.TryParse(value, out IPAddress n)) continue;
                    }
                    else if (property.Type == "bool" || property.Type == "boolean")
                    {
                        if (value.ToLower() != "true" && value.ToLower() != "false" && value != "1" && value != "0") continue;
                    }
                    else
                    {
                        JToken token = null;
                        try
                        {
                            token = XiYouSerializer.DeserializeJson<JToken>(value);
                        }
                        catch (Exception)
                        {
                            return true;
                        }
                        if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                        {
                            return true;
                        }
                        if (property.Type == "json" || property.Type == "jtoken") continue;
                        if (property.Type == "jarray" && token is JArray) continue;
                        if (property.Type == "jobject" && token is JObject) continue;
                        if (property.Type == "jvalue" && token is JValue) continue;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                tips = "exception";
                return true;
            }
        }

        public static bool CheckInternalFirewall(HttpContext context)
        {
            try
            {
                IConfigurationSection configuration = GetConfiguration().GetSection("InternalFirewall");
                int allowOrigin = configuration.GetValue<int>("AllowOrigin");
                if (0 == allowOrigin)
                {
                    return true;
                }

                IPAddress sourceAddress = XiYouUtility.GetRemoteIpAddress(context);
                if (null == sourceAddress)
                {
                    return false;
                }

                // 来自于本机IP的访问都是受信的。
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni == null)
                    {
                        continue;
                    }
                    var ipp = ni.GetIPProperties();
                    if (ipp.UnicastAddresses.FirstOrDefault(ua => Buffers.memcmp(ua.Address, sourceAddress) == 0) != null)
                    {
                        return true;
                    }
                }

                long addressNum = 0;
                byte[] addressBuffer = sourceAddress.GetAddressBytes();
                if (null == addressBuffer)
                {
                    return false;
                }
                if (addressBuffer.Length == 4)
                {
                    addressNum = BitConverter.ToInt32(addressBuffer, 0);
                }
                if (addressBuffer.Length == 8)
                {
                    addressNum = BitConverter.ToInt64(addressBuffer, 0);
                }
                if (0 == addressNum) // 不允许0.0.0.0这样的数字地址存在
                {
                    return false;
                }

                string hostName = sourceAddress.ToString();
                try
                {
                    hostName = Dns.GetHostEntry(sourceAddress)?.HostName;
                }
                catch (Exception)
                {
                    /*无法反向解析主机名*/
                }

                hostName = hostName.Trim().ToLower();
                if (allowOrigin >= 3)
                {
                    return true;
                }
                else // https://github.com/liulilittle/PaperAirplane/blob/master/src/relay%20server/SocketProxyChecker.cpp
                {
                    if (sourceAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
#pragma warning disable CS0618 // 类型或成员已过时
                        if (Buffers.memcmp(sourceAddress, IPAddress.Loopback) == 0 
                            || Buffers.memcmp(sourceAddress, IPAddress.Any) == 0) // 此不是一个有效的环路
#pragma warning restore CS0618 // 类型或成员已过时
                        {
                            return true;
                        }
                    }
                    else if (sourceAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
#pragma warning disable CS0618 // 类型或成员已过时
                        if (Buffers.memcmp(sourceAddress, IPAddress.IPv6Loopback) == 0
                            || Buffers.memcmp(sourceAddress, IPAddress.IPv6Any) == 0)
#pragma warning restore CS0618 // 类型或成员已过时
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                    if (allowOrigin == 1)
                    {
                        if (addressBuffer[0] == 127) // L
                        {
                            return true;
                        }
                        if (addressBuffer[0] > 223) // E
                        {
                            return true;
                        }
                        if (addressBuffer[0] == 10) // A
                        {
                            return true;
                        }
                        if (addressBuffer[0] == 192 && addressBuffer[1] == 168) // C
                        {
                            return true;
                        }
                        if (addressBuffer[0] == 172 && addressBuffer[1] >= 16 && addressBuffer[1] <= 131) // B
                        {
                            return true;
                        }
                    }
                }

                string[] exceptionHosts = configuration.GetSection("ExceptionHosts").Get<string[]>();
                if (exceptionHosts == null || exceptionHosts.Length <= 0)
                {
                    return false;
                }

                string remoteAddress = sourceAddress.ToString();
                for (int i = 0; i < exceptionHosts.Length; i++)
                {
                    string hosts = exceptionHosts[i] ?? string.Empty;
                    hosts = hosts.Trim().ToLower();
                    if (hosts == hostName || hosts == remoteAddress)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void ProcessRequest(HttpContext context) // 反向代理
        {
            try
            {
                if (context == null)
                {
                    return;
                }
                context.Response.StatusCode = 200;
            }
            catch (Exception)
            {
                return;
            }

            if (!CheckIsInvokeGmWebApi(context))
            {
                return;
            }
            else
            {
                context.Request.Files.Clear();
            }

            if (!CheckInternalFirewall(context))
            {
                this.ResponseWriteError(context, "UnableToPassThroughFirewall");
                return;
            }

            GMRequestMessage message = GMRequestMessage.GetMessage(context);
            if (message == null)
            {
                this.ResponseWriteError(context, "GMWebProtocolHeaderError");
                return;
            }

            if (CheckMessageIsTimeout(message))
            {
                this.ResponseWriteError(context, "TimespanIsTimeout");
                return;
            }

            GMInterface interfaces = GetInterfaces(message.action);
            if (interfaces == null)
            {
                this.ResponseWriteError(context, "InterfaceIsUndefined");
            }
            else if (!CheckHttpMethod(context, interfaces.Method))
            {
                this.ResponseWriteError(context, "HttpMethodNotAvailable");
            }
            else
            {
                if (CheckMessageIsInvalidSignature(message))
                {
                    this.ResponseWriteError(context, "InvalidSignatureText");
                    return;
                }
                else if (!message.Fill())
                {
                    this.ResponseWriteError(context, "UnableToResolveParameter");
                    return;
                }
                string tips = "";
                if (CheckMessageIsLimitingError(message, interfaces, ref tips))
                {
                    this.ResponseWriteError(context, "ArgumentOutOfLimiting", tips);
                    return;
                }
                string request = FetchRequestToGsGmILProtocol(message, interfaces);
                if (request == null)
                {
                    this.ResponseWriteError(context, "InvalidRequestArgument");
                }
                else
                {
                    this.HandleWebInvoke(context, message, request);
                }
            }
        }

        private bool ResponseWriteError(HttpContext context, string category, string append = "")
        {
            if (null == context || string.IsNullOrEmpty(category))
            {
                return false;
            }
            var conf = GetConfiguration().GetSection("Error").GetSection(category);
            if (null == conf)
            {
                return false;
            }
            GenericResponse<object> response = new GenericResponse<object>();
            response.Tag = conf.GetValue<object>("Tag");
            response.Code = conf.GetValue<int>("Code");
            if (append.Length > 0)
            {
                response.Message = conf.GetValue<string>("Message") + "_" + append;
            }
            else
            {
                response.Message = conf.GetValue<string>("Message");
            }
            return context.Response.Write(XiYouSerializer.SerializableJson(response));
        }

        private void ResponseWebClient(HttpContext context, SocketReuqestInvokerError error, GenericResponse<string> result)
        {
            if (result == null)
            {
                if (error == SocketReuqestInvokerError.Timeout)
                {
                    ResponseWriteError(context, "RetryAckTimeout");
                }
                else if (error == SocketReuqestInvokerError.Success)
                {
                    ResponseWriteError(context, "AckCorrectButNotResponse");
                }
                else
                {
                    ResponseWriteError(context, "SeriousLinklineLayerFailure");
                }
            }
            else
            {
                GenericResponse<object> response = new GenericResponse<object>();
                if (result.Tag == null)
                {
                    response.Code = result.Code;
                    response.Tag = null;
                    response.Message = result.Message;
                }
                else
                {
                    response.Code = result.Code;
                    response.Message = result.Message;
                    if (result.Tag.Length <= 0)
                    {
                        response.Tag = result.Tag;
                    }
                    else
                    {
                        try
                        {
                            response.Tag = XiYouSerializer.DeserializeJson<object>(result.Tag);
                        }
                        catch (Exception)
                        {
                            response.Tag = result.Tag;
                        }
                    }
                }
                context.Response.Write(XiYouSerializer.SerializableJson(response));
            }
            context.Response.End();
        }

        private void HandleWebInvoke(HttpContext context, GMRequestMessage message, string request)
        {
            if (message.sids == 0) // 广播交换到已知的区域游戏服务器之中（exchange-topic-model）
            {
                if (MainApplication.SocketMvhApplication is GMWebSocketMvhApplication)
                {
                    foreach (long identifier in (MainApplication.SocketMvhApplication as GMWebSocketMvhApplication).GetAllAvailableIdentifier())
                    {
                        if (identifier == 0)
                        {
                            continue;
                        }
                        HandleWebInvoke(context, identifier, message, request, message.platform);
                    }
                }
                else
                {
                    foreach (ISocketClient socket in MainApplication.SocketMvhApplication.GetAllClient(message.platform))
                    {
                        long identifier = MainApplication.SocketMvhApplication.GetIdentifier(socket);
                        if (identifier == 0)
                        {
                            continue;
                        }
                        HandleWebInvoke(context, identifier, message, request, message.platform);
                    }
                }
            }
            else
            {
                HandleWebInvoke(context, message.sids, message, request, message.platform);
            }
        }

        private void HandleWebInvoke(HttpContext context, long identifier, GMRequestMessage message, string request, string platform)
        {
            Console.WriteLine("{0}|request = {1} - {2}", DateTime.Now.ToString(), message.action, request);
            SocketMvhApplication mvh = MainApplication.SocketMvhApplication;
            if (mvh.Invoker.InvokeAsync<GenericResponse<string>>(platform, identifier, (ushort)XiYouSdkCommands.XiYouSdkCommands_GMWebInstruction,
                new GMWebInstruction()
                {
                    Interface = message.action,
                    Request = request ?? string.Empty,
                    Platform = message.platform,
                    TimeSpan = message.timestamp
                },
                (error, model) => ResponseWebClient(context, error, model)))
            {
                context.Asynchronous = true; // 异步响应请求，可能长时间挂起
            }
            else
            {
                this.ResponseWriteError(context, "CanNotCallInNonAck");
            }
        }
    }
}
