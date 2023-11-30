namespace GVMServer.Net.Web.Mvc
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using GVMServer.Net.Web;
    using GVMServer.Net.Web.Mvc.Controller;

    public class MvcApplication
    {
        private HttpApplication m_application = null;
        internal ConcurrentDictionary<IHttpHandler, IHttpHandler> m_handlers = new ConcurrentDictionary<IHttpHandler, IHttpHandler>();

        public string Root
        {
            get
            {
                return m_application.Root;
            }
            set
            {
                m_application.Root = value;
            }
        }

        public ControllerContainer Controllers
        {
            get;
            private set;
        }

        public MvcApplication()
        {
            Controllers = new ControllerContainer();
            m_application = new HttpApplication
            {
                Handler = new MvcHandler(this)
            };
        }

        public void AddHandler(IHttpHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            m_handlers.TryAdd(handler, handler);
        }

        public void Start(params string[] prefixes)
        {
            m_application.Start(prefixes);
        }

        public void Start(IList<string> prefixes)
        {
            m_application.Start(prefixes);
        }

        public void Stop()
        {
            m_application.Stop();
        }
    }
}
