﻿namespace GVMServer.Net.Web
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Web;

    public class HttpRequest
    {
        private HttpListenerRequest request = null;
        private string path = null;
        private HttpForm form = null;
        private MemoryStream uploadStream = null;
        private NameValueCollection queryString = null;

        public HttpContext CurrentContext
        {
            get;
            internal set;
        }

        internal HttpRequest(HttpListenerRequest request)
        {
            this.request = request;
        }

        public string HttpMethod
        {
            get { return request.HttpMethod; }
        }

        public string Path
        {
            get
            {
                if (path == null)
                {
                    Uri raw = request.Url;
                    path = raw.AbsolutePath;
                }
                return path;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                return request.Cookies;
            }
        }

        public bool IsLocal
        {
            get
            {
                return request.IsLocal;
            }
        }

        public string RawUrl
        {
            get
            {
                return request.RawUrl;
            }
        }

        public string UserAgent
        {
            get
            {
                return request.UserAgent;
            }
        }

        public string[] UserLanguages
        {
            get
            {
                return request.UserLanguages;
            }
        }

        public string UserHostName
        {
            get
            {
                return request.UserHostName;
            }
        }

        public string UserHostAddress
        {
            get
            {
                return request.UserHostAddress;
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                return request.UrlReferrer;
            }
        }

        public Uri Url
        {
            get
            {
                return request.Url;
            }
        }

        public string[] AcceptTypes
        {
            get
            {
                return request.AcceptTypes;
            }
        }

        public string ContentType
        {
            get
            {
                return request.ContentType;
            }
        }

        public long ContentLength
        {
            get
            {
                return request.ContentLength64;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                return request.ContentEncoding;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return request.KeepAlive;
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                return request.ProtocolVersion;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return request.LocalEndPoint;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return request.RemoteEndPoint;
            }
        }

        public string ServiceName
        {
            get
            {
                return request.ServiceName;
            }
        }

        public Guid RequestTraceIdentifier
        {
            get
            {
                return request.RequestTraceIdentifier;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                lock (this)
                {
                    if (this.queryString == null)
                    {
                        this.queryString = new NameValueCollection();
                        Uri url = this.Url;
                        string query = url.Query;
                        do
                        {
                            if (string.IsNullOrEmpty(query))
                            {
                                break;
                            }
                            if (query[0] == '?')
                            {
                                if (query.Length <= 1)
                                {
                                    break;
                                }
                                query = query.Substring(1);
                            }
                            if (!string.IsNullOrEmpty(query))
                            {
                                foreach (string kv in query.Split('&'))
                                {
                                    int i = kv.IndexOf('=');
                                    string key = null;
                                    string value = null;
                                    if (i <= -1)
                                    {
                                        key = kv;
                                    }
                                    else
                                    {
                                        key = kv.Substring(0, i);
                                        value = kv.Substring(i + 1);
                                    }
                                    key = HttpUtility.UrlDecode(key);
                                    value = HttpUtility.UrlDecode(value);
                                    this.queryString.Add(key, value);
                                }
                            }
                        } while (false);
                    }
                }
                return this.queryString;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return request.Headers;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                if (form == null)
                {
                    form = HttpForm.Resolve(this);
                }
                return form.Form;
            }
        }

        public HttpFileCollection Files
        {
            get
            {
                if (form == null)
                {
                    form = HttpForm.Resolve(this);
                }
                return form.Files;
            }
        }

        public Stream InputStream
        {
            get
            {
                if (uploadStream == null)
                {
                    byte[] src = new byte[request.ContentLength64];
                    uploadStream = new MemoryStream(src);
                    request.InputStream.CopyTo(uploadStream);
                    uploadStream.Seek(0, SeekOrigin.Begin);
                }
                return uploadStream;
            }
        }

        public string this[string key]
        {
            get
            {
                string value = this.QueryString[key];
                if (value != null)
                {
                    return value;
                }
                value = this.Form[key];
                if (value != null)
                {
                    return value;
                }
                Cookie cookie = this.Cookies[key];
                if (cookie == null)
                {
                    return null;
                }
                return cookie.ToString();
            }
        }
    }
}
