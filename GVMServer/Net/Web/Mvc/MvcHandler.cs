namespace GVMServer.Net.Web.Mvc
{
    using GVMServer.Net.Web;
    using GVMServer.Net.Web.Mvc.Controller;

    public class MvcHandler : IHttpHandler
    {
        private MvcApplication m_application = null;

        internal MvcHandler(MvcApplication application)
        {
            m_application = application;
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;
            HttpActionContext action = m_application.Controllers.Get(request.HttpMethod, request.Path);
            if (action == null)
            {
                response.StatusCode = 404;
            }
            else
            {
                response.ContentType = "text/plain";
                response.StatusCode = 200;
                var controller = action.Controller;
                HttpMethodAttribute attr = action.Method;
                object result = (action.ModelType == typeof(HttpContext) || action.ModelType == typeof(object) ?
                    action.Invoke(() => context) :
                    action.Invoke(() => controller.DeserializeInputModel(action.ModelType, attr)));
                if (action.ActionReturnType != typeof(void))
                {
                    controller.Write(result);
                }
            }
            foreach (IHttpHandler handler in m_application.m_handlers.Values)
            {
                handler.ProcessRequest(context);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}
