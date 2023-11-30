namespace GVMServer.Net.Web.Mvc.Controller
{
    using System;

    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute(string path) : base(path)
        {

        }

        public override string HttpMethod
        {
            get
            {
                return "POST";
            }
        }
    }
}
