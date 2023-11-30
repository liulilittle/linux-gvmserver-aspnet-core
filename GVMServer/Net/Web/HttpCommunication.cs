namespace GVMServer.Net.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;

    class HttpCommunication
    {
        private HttpListener server = null;
        private readonly object locker = new object();

        public Action<object, HttpListenerContext> Received
        {
            get;
            set;
        }

        ~HttpCommunication()
        {
            lock (this.locker)
            {
                using (HttpListener listener = this.server)
                {
                    if (listener != null)
                    {
                        listener.Close();
                    }
                    this.server = null;
                }
            }
            GC.SuppressFinalize(this);
        }

        private void InternalGetContextAsyncCycleLooper(IAsyncResult result)
        {
            do
            {
                if (server == null)
                {
                    break;
                }
                else if (result == null)
                {
                    try
                    {
                        server.BeginGetContext(InternalGetContextAsyncCycleLooper, null);
                    }
                    catch (Exception) { break; }
                }
                else
                {
                    HttpListenerContext context = null;
                    try
                    {
                        context = server.EndGetContext(result);
                    }
                    catch (Exception) { break; }
                    Action<object, HttpListenerContext> received = this.Received;
                    if (received != null)
                    {
                        received(this, context);
                    }
                    this.InternalGetContextAsyncCycleLooper(null);
                }
            } while (false);
        }

        public void Start(IList<string> prefixes)
        {
            lock (this.locker)
            {
                if (this.server != null)
                {
                    throw new InvalidOperationException();
                }

                HttpListener listener = new HttpListener();
                listener.IgnoreWriteExceptions = true;
                //listener.UnsafeConnectionNtlmAuthentication = true;
                //listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                foreach (string prefixe in prefixes)
                {
                    if (string.IsNullOrEmpty(prefixe))
                    {
                        continue;
                    }

                    listener.Prefixes.Add(prefixe);
                }
                try
                {
                    listener.Start();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                this.server = listener;
                this.InternalGetContextAsyncCycleLooper(null);
            }
        }

        public void Stop()
        {
            lock (this.locker)
            {
                HttpListener listener = this.server;
                if (listener != null)
                {
                    try
                    {
                        listener.Prefixes.Clear();
                        listener.Abort();
                        listener.Stop();
                    }
                    catch (Exception) { }
                    try
                    {
                        listener.Close();
                    }
                    catch (Exception) { }
                }

                this.server = null;
            }
        }
    }
}
