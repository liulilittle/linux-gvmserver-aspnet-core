namespace GVMServer.Net.Web
{
    public interface IHttpHandler
    {
        void ProcessRequest(HttpContext context);
    }
}
