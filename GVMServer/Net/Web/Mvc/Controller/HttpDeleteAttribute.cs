namespace GVMServer.Net.Web.Mvc.Controller
{
    using System;

    public sealed class HttpDeleteAttribute : HttpMethodAttribute
    {
        public HttpDeleteAttribute(string path) : base(path)
        {

        }

        public override string HttpMethod
        {
            get
            {
                return "DELETE";
            }
        }
    }
}
