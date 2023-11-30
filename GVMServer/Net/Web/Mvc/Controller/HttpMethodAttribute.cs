namespace GVMServer.Net.Web.Mvc.Controller
{
    using System;
    using System.Reflection;

    public abstract class HttpMethodAttribute : Attribute
    {
        private string m_mappingPathValue = null;

        public HttpMethodAttribute(string path)
        {
            m_mappingPathValue = path;
        }

        public string MappingPathValue
        {
            get
            {
                return m_mappingPathValue;
            }
        }

        public abstract string HttpMethod
        {
            get;
        }

        public static HttpMethodAttribute Get(MemberInfo m)
        {
            if (m == null)
                return null;
            object[] attrs = m.GetCustomAttributes(typeof(HttpMethodAttribute), true);
            if (attrs == null || attrs.Length <= 0)
                return null;
            return (HttpMethodAttribute)attrs[0];
        }
    }
}
