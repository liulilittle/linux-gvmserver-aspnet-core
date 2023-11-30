namespace GVMServer.W3Xiyou.Docking
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Net.Cache;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using GVMServer.Cryptography.Final;
    using GVMServer.Net.Web;
    using GVMServer.Serialization;
    using GVMServer.Threading;
    using Microsoft.Extensions.Configuration;
    using Interlocked = System.Threading.Interlocked;

    public static class XiYouUtility
    {
        private static CookieContainer defaultCookieContainer = new CookieContainer();

        public unsafe static string UrlEncode(string s)
        {
            return UrlEncode(s, Encoding.UTF8);
        }

        public static Encoding GetDefaultEncoding()
        {
            return BinaryFormatter.DefaultEncoding;
        }

        public static string Es8(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            try
            {
                Encoding encoding = XiYouUtility.GetDefaultEncoding();
                if (null != encoding && encoding != Encoding.UTF8)
                {
                    byte[] buffer = encoding.GetBytes(s);
                    if (XiYouUtility.IsUTF8Buffer(buffer))
                    {
                        return Encoding.UTF8.GetString(buffer);
                    }
                }
                return s;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public unsafe static bool IsUTF8Buffer(byte[] buffer)
        {
            if (buffer == null)
            {
                return false;
            }
            fixed (byte* pinned = buffer)
            {
                return IsUTF8Buffer(pinned, buffer.Length);
            }
        }

        public unsafe static bool IsUTF8Buffer(byte* buffer, int count)
        {
            if (buffer == null || count <= 0)
            {
                return false;
            }
            int counter = 1;
            byte key = 0;
            for (int i = 0; i < count; i++)
            {
                key = buffer[i];
                if (counter == 1)
                {
                    if (key >= 0x80)
                    {
                        while (((key <<= 1) & 0x80) != 0)
                        {
                            counter++;
                        }
                        if (counter == 1 || counter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if ((key & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    counter--;
                }
            }
            return !(counter > 1);
        }

        public static uint GetIPV4Address(IPAddress address)
        {
            if (address == null)
            {
                return 0;
            }
            byte[] buf = address.GetAddressBytes();
            int offset = 0;
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                offset = buf.Length - sizeof(uint);
                if (offset < 0)
                {
                    return 0;
                }
            }
            return BitConverter.ToUInt32(buf, offset);
        }

        public static bool IsPostBack(this string httpMethod)
        {
            if (string.IsNullOrEmpty(httpMethod))
                return false;
            return Regex.IsMatch(httpMethod, "POST", RegexOptions.IgnoreCase);
        }

        public static long ToTimeSpan13(DateTime time)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime,
                TimeZoneInfo.Local); // TimeZone.CurrentTimeZone.ToLocalTime( dateTime );
            long ts = unchecked(time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
            return ts;
        }

        public static uint ToTimeSpan10(DateTime time)
        {
            return Convert.ToUInt32(ToTimeSpan13(time) / 1000);
        }

        public unsafe static string UrlEncode(string s, Encoding encoding) // Google UrlEncode
        {
            if (string.IsNullOrEmpty(s)) // HttpUtility.UrlEncode(s, Encoding.UTF8);
            {
                return string.Empty;
            }
            string eax = string.Empty;
            StringBuilder sb = new StringBuilder();

            byte[] data = encoding.GetBytes(s);
            fixed (byte* pinned = data)
            {
                sbyte* p = (sbyte*)pinned;
                for (int i = 0, len = data.Length; i < len; i++, p++)
                {
                    int asc = *p < 0 ? 256 + *p : *p;
                    if (asc < 42 || asc == 43 || asc > 57 && asc < 64 || asc > 90 && asc < 95 || asc == 96 || asc > 122)
                    {
                        eax = Convert.ToString(asc, 16);
                        sb.Append(eax.Length < 2 ? "%0" + eax : "%" + eax);
                    }
                    else
                    {
                        sb.Append((char)asc);
                    }
                }
            }
            return sb.ToString();
        }

        public static string UrlDecode(string s)
        {
            return UrlDecode(s, Encoding.UTF8);
        }

        public static string UrlDecode(string s, Encoding encoding)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return HttpUtility.UrlDecode(s, encoding);
        }

        public static NameValueCollection UrlDecode(NameValueCollection s)
        {
            if (s == null)
            {
                return null;
            }
            NameValueCollection results = new NameValueCollection();
            if (s.Count > 0)
            {
                foreach (string key in s)
                {
                    string value = s.Get(key);
                    value = XiYouUtility.UrlDecode(value);
                    results.Add(key, value);
                }
            }
            return results;
        }

        public static string SignText(string signStr, XiYouSdkConfiguration configuration)
        {
            if (configuration == null || string.IsNullOrEmpty(signStr))
            {
                return string.Empty;
            }
            string appSecret = configuration.AppSecret;
            if (string.IsNullOrEmpty(appSecret))
            {
                return string.Empty;
            }
            string inStr = (signStr + appSecret);
            string sign = MD5.ToMD5String(inStr, Encoding.UTF8);
            if (!configuration.SignToLower)
            {
                return sign;
            }
            return sign.ToLower();
        }

        public static string SignText(string s, int category)
        {
            XiYouSdkConfiguration configuration = XiYouSdkConfiguration.GetConfiguration(category);
            if (configuration == null)
            {
                return string.Empty;
            }
            return SignText(s, configuration);
        }

        public static bool EqualsAndIgnoreCast(this string x, string value)
        {
            if (x == null && value != null)
            {
                return false;
            }
            if (value == null && x != null)
            {
                return false;
            }
            if (x == value)
            {
                return true;
            }
            if (x.Length != value.Length)
            {
                return false;
            }
            return x.ToLower() == value.ToLower();
        }

        public enum PostAsyncError
        {
            kSuccess,
            kTimeout,
            kDisconnect,
            kCancelled,
            kUnableToCreateWebRequest,
            kUnableToOpenWebRequest,
            kUnableToGetRequestStream,
            kUnableToWriteToRequestStream,
            kUnableToGetResponse,
            kResponseNotHttpProtocol,
            kResponseStateCodeNotEqualsOK,
            kUnableToDeserializeJson,
            kUnableToReadResponseContentToEnd,
            kUnableToFillRequestHeaders,
            kUnableToReadResponseStream,
            kUnableToCloseResponseStream,
            kUnableToCloseResponseInstance,
            kServerInternalError,
        };

        public delegate void PostAsyncCallback<T>(PostAsyncError error, T response);

        public static int DefaultTimeout { get; set; } = 3000;

        public static bool PostToUrlAsync(string rawUrl, string message, PostAsyncCallback<HttpWebResponse> callback, int timeout)
        {
            return PostToUrlAsync(rawUrl, message, callback, null, timeout);
        }

        public static bool PostToUrlAsync<T>(string rawUrl, string message, PostAsyncCallback<T> callback, int timeout)
        {
            return PostToUrlAsync(rawUrl, message, callback, null, timeout);
        }

        public static bool PostToUrlAsync<T>(string rawUrl, string message, PostAsyncCallback<T> callback, Action<HttpWebRequest> settings, int timeout)
        {
            PostAsyncCallback<HttpWebResponse> pcb = null;
            if (callback != null)
            {
                pcb = (error, response) =>
                {
                    T out_ = default(T);
                    try
                    {
                        if (response != null)
                        {
                            string contents = null;
                            Stream stream = null;
                            try
                            {
                                stream = response.GetResponseStream();
                            }
                            catch (Exception)
                            {
                                error = PostAsyncError.kUnableToGetResponse;
                            }
                            if (stream != null)
                            {
                                try
                                {
                                    bool readToEnd = false;
                                    using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        try
                                        {
                                            contents = sr.ReadToEnd();
                                            readToEnd = true;
                                        }
                                        catch (Exception)
                                        {
                                            error = PostAsyncError.kUnableToReadResponseContentToEnd;
                                        }
                                    }
                                    stream.Dispose();
                                    if (readToEnd)
                                    {
                                        try
                                        {
                                            out_ = XiYouSerializer.DeserializeJson<T>(contents);
                                        }
                                        catch (Exception)
                                        {
                                            error = PostAsyncError.kUnableToDeserializeJson;
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    error = PostAsyncError.kUnableToReadResponseStream;
                                    try
                                    {
                                        stream.Dispose();
                                    }
                                    catch (Exception)
                                    {
                                        error = PostAsyncError.kUnableToCloseResponseStream;
                                    }
                                }
                            }
                            try
                            {
                                response.Dispose();
                            }
                            catch (Exception)
                            {
                                error = PostAsyncError.kUnableToCloseResponseInstance;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        error = PostAsyncError.kServerInternalError;
                    }
                    callback(error, out_);
                };
            }
            return PostToUrlAsync(rawUrl, message, pcb, timeout);
        }

        public static bool PostToUrlAsync(string rawUrl, string message, PostAsyncCallback<HttpWebResponse> callback, Action<HttpWebRequest> settings, int timeout = -1)
        {
            if (string.IsNullOrEmpty(rawUrl) || (timeout == 0 || timeout < -1))
            {
                return false;
            }
            try
            {
                HttpWebRequest request = null;
                try
                {
                    request = WebRequest.CreateHttp(rawUrl);
                }
                catch (Exception)
                {
                    callback?.Invoke(PostAsyncError.kUnableToCreateWebRequest, null);
                    return false;
                }
                byte[] messageBuffer = null;
                try
                {
                    request.Method = "POST";
                    request.Proxy = null;
                    request.ContentType = "application/x-www-form-urlencoded"; // 老旧的表单投递协议

                    messageBuffer = Encoding.UTF8.GetBytes(message);
                    request.ContentLength = messageBuffer.Length;
                    request.CookieContainer = defaultCookieContainer;

                    request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                    request.KeepAlive = true;
                    request.Referer = rawUrl;
                    request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.117 Safari/537.36";
                    request.Headers.Add("Accept-Encoding: gzip, deflate");
                    request.Headers.Add("DNT: 1");
                    request.Headers.Add("Upgrade-Insecure-Requests: 1");
                    request.Headers.Add("Accept-Language: zh-cn,zh;q=0.8,en-us;q=0.5,en;q=0.3");

                    if (timeout > 0)
                    {
                        request.Timeout = timeout;
                    }
                }
                catch (Exception)
                {
                    callback?.Invoke(PostAsyncError.kUnableToFillRequestHeaders, null);
                    return false;
                }
                Action<Exception, PostAsyncError> onError = (e, error) =>
                {
                    if (e != null)
                    {
                        if (e is TimeoutException)
                        {
                            error = PostAsyncError.kTimeout;
                        }
                    }
                    callback?.Invoke(error, null);
                };
                settings?.Invoke(request);
                try
                {
                    request.BeginGetRequestStream((ar0) =>
                    {
                        bool success = false;
                        try
                        {
                            using (Stream stream = request.EndGetRequestStream(ar0))
                            {
                                try
                                {
                                    stream.Write(messageBuffer, 0, messageBuffer.Length);
                                    stream.Flush(); // 立即写入缓存到INTETNERT上（此处可能发生异常）
                                    stream.Close(); // 此处可能发生异常
                                    success = true;
                                }
                                catch (Exception e) // 这里可能链接突然中断
                                {
                                    onError(e, PostAsyncError.kUnableToWriteToRequestStream);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            onError(e, PostAsyncError.kUnableToGetRequestStream);
                        }
                        if (success)
                        {
                            try
                            {
                                request.BeginGetResponse((ar1) =>
                                {
                                    HttpWebResponse response = null;
                                    success = false;
                                    try
                                    {
                                        WebResponse backResponse = request.EndGetResponse(ar1);
                                        response = backResponse as HttpWebResponse;
                                        if (response == null) // 可能是FTP的协议，为了避免这个问题所以此时
                                        {
                                            backResponse.Dispose();
                                            onError(null, PostAsyncError.kResponseNotHttpProtocol);
                                        }
                                        else if (response.StatusCode != HttpStatusCode.OK)
                                        {
                                            onError(null, PostAsyncError.kResponseStateCodeNotEqualsOK);
                                        }
                                        else
                                        {
                                            success = true;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        onError(e, PostAsyncError.kDisconnect);
                                    }
                                    if (success)
                                    {
                                        callback?.Invoke(PostAsyncError.kSuccess, response);
                                    }
                                }, null);
                            }
                            catch (Exception e)
                            {
                                onError(e, PostAsyncError.kDisconnect);
                            }
                        }
                    }, null);
                    return true;
                }
                catch (Exception)
                {
                    callback?.Invoke(PostAsyncError.kUnableToOpenWebRequest, null);
                    return false;
                }
            }
            catch (Exception)
            {
                callback?.Invoke(PostAsyncError.kServerInternalError, null);
                return false;
            }
        }

        public static void GetFromUrlAsync<T>(string rawUri, int timeout, Action<PostAsyncError, T> callback)
        {
            if (callback == null || string.IsNullOrEmpty(rawUri))
            {
                return;
            }

            DownloadStringCompletedEventHandler onDownloadStringCompleted = null;
            EventHandler onTimeoutTick = null;
            WebClient webClient = new WebClient();
            Timer timeoutTimer = new Timer(timeout);
            int calledcount = 0;
            try
            {
                onDownloadStringCompleted = (sender, e) =>
                {
                    timeoutTimer.Dispose();
                    webClient.DownloadStringCompleted -= onDownloadStringCompleted;
                    webClient.Dispose();
                    if (e.Cancelled)
                    {
                        if (Interlocked.Increment(ref calledcount) <= 1)
                            callback(PostAsyncError.kCancelled, default(T));
                    }
                    else if (e.Error != null)
                    {
                        if (Interlocked.Increment(ref calledcount) <= 1)
                            callback(PostAsyncError.kServerInternalError, default(T));
                    }
                    else
                    {
                        T response = default(T);
                        if (string.IsNullOrEmpty(e.Result))
                        {
                            if (Interlocked.Increment(ref calledcount) <= 1)
                                callback(PostAsyncError.kUnableToReadResponseContentToEnd, default(T));
                        }
                        else
                        {
                            try
                            {
                                response = XiYouSerializer.DeserializeJson<T>(e.Result);
                            }
                            catch (Exception)
                            {

                            }
                        }
                        if (response == null)
                        {
                            if (Interlocked.Increment(ref calledcount) <= 1)
                                callback(PostAsyncError.kUnableToDeserializeJson, default(T));
                        }
                        else
                        {
                            if (Interlocked.Increment(ref calledcount) <= 1)
                                callback(PostAsyncError.kSuccess, response);
                        }
                    }
                };
                onTimeoutTick = (sender, e) =>
                {
                    webClient.DownloadStringCompleted -= onDownloadStringCompleted;
                    webClient.CancelAsync();
                    webClient.Dispose();
                    timeoutTimer.Dispose();
                };
                do
                {
                    timeoutTimer.Tick += onTimeoutTick;
                    webClient.DownloadStringCompleted += onDownloadStringCompleted;
                    webClient.DownloadStringAsync(new Uri(rawUri));
                    timeoutTimer.Start();
                } while (false);
            }
            catch (Exception)
            {
                if (Interlocked.Increment(ref calledcount) <= 1)
                    callback(PostAsyncError.kUnableToCreateWebRequest, default(T));
                do
                {
                    webClient.DownloadStringCompleted -= onDownloadStringCompleted;
                    webClient.CancelAsync();
                    webClient.Dispose();
                    timeoutTimer.Dispose();
                } while (false);
            }
        }

        public static T PostToUrl<T>(string rawUrl, string message, int timeout)
        {
            return PostToUrl<T>(rawUrl, message, null, timeout);
        }

        public static T PostToUrl<T>(string rawUrl, string message, Action<HttpWebRequest> settings, int timeout)
        {
            if (string.IsNullOrEmpty(rawUrl) || (timeout == 0 || timeout < -1))
            {
                return default(T);
            }
            HttpWebRequest request = null;
            byte[] messageBuffer = null;
            try
            {
                request = WebRequest.CreateHttp(rawUrl);

                request.Method = "POST";
                request.Proxy = null;
                request.ContentType = "application/x-www-form-urlencoded"; // 老旧的表单投递协议

                messageBuffer = Encoding.UTF8.GetBytes(message);
                request.ContentLength = messageBuffer.Length;
                request.CookieContainer = defaultCookieContainer;

                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.KeepAlive = true;
                request.Referer = rawUrl;
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.117 Safari/537.36";
                request.Headers.Add("Accept-Encoding: gzip, deflate");
                request.Headers.Add("DNT: 1");
                request.Headers.Add("Upgrade-Insecure-Requests: 1");
                request.Headers.Add("Accept-Language: zh-cn,zh;q=0.8,en-us;q=0.5,en;q=0.3");

                if (timeout > 0)
                {
                    request.Timeout = timeout;
                }
            }
            catch (Exception)
            {
                return default(T);
            }
            try
            {
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(messageBuffer, 0, messageBuffer.Length);
                    stream.Flush(); // 立即写入缓存到INTETNERT上（此处可能发生异常）
                    stream.Close(); // 此处可能发生异常
                }
            }
            catch (Exception) // 这里可能链接突然中断
            {
                return default(T);
            }
            try
            {
                using (WebResponse backResponse = request.GetResponse())
                {
                    try
                    {
                        HttpWebResponse response = backResponse as HttpWebResponse;
                        if (response == null)
                        {
                            return default(T);
                        }
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream stream = response.GetResponseStream())
                            {
                                using (StreamReader sr = new StreamReader(stream))
                                {
                                    try
                                    {
                                        string contents = sr.ReadToEnd();
                                        if (typeof(T) == typeof(string))
                                        {
                                            return (T)(object)contents;
                                        }
                                        else
                                        {
                                            return XiYouSerializer.DeserializeJson<T>(contents);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        return default(T);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return default(T);
                    }
                }
            }
            catch (Exception)
            {
                return default(T);
            }
            return default(T);
        }

        public static IPAddress GetRemoteIpAddress(this HttpContext context)
        {
            try
            {
                IConfigurationSection config = MainApplication.GetDefaultConfiguration().GetSection("Web");
                IPAddress address = null;
                if (config.GetValue<bool>("GetIPAddressFromHttpHeaders"))
                {
                    //proxy_set_header Host $host;
                    //proxy_set_header X-Real - IP $remote_addr;
                    //proxy_set_header REMOTE-HOST $remote_addr;
                    //proxy_set_header X-Forwarded - For $proxy_add_x_forwarded_for;

                    string remote_addr = context.Request.Headers["X-Real-IP"];
                    if (string.IsNullOrEmpty(remote_addr))
                    {
                        remote_addr = context.Request.Headers["REMOTE-HOST"];
                    }
                    if (string.IsNullOrEmpty(remote_addr))
                    {
                        remote_addr = context.Request.Headers["X-Forwarded-For"];
                    }
                    if (!string.IsNullOrEmpty(remote_addr))
                    {
                        IPAddress.TryParse(remote_addr, out address);
                    }
                    if (address == null)
                    {
                        address = IPAddress.Any;
                    }
                }
                else
                {
                    address = context.Request.RemoteEndPoint?.Address;
                }
                return address;
            }
            catch (Exception)
            {
                return IPAddress.Any;
            }
        }
    }
}
