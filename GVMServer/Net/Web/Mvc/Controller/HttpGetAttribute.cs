namespace GVMServer.Net.Web.Mvc.Controller
{
    public class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute(string path) : base(path)
        {

        }

        public override string HttpMethod
        {
            get
            {
                return "GET";
            }
        }
    }
}
