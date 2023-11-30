namespace GVMServer.Net.Web.Mvc.Controller
{
    using System;
    using System.Collections.Specialized;
    using GVMServer.Net.Web;
    using GVMServer.Serialization;
    using GVMServer.W3Xiyou.Docking;

    public abstract class Controller
    {
        public T Deserialize<T>(NameValueCollection values)
        {
            return JsonSerializer.Deserialize<T>(values);
        }

        public object Deserialize(NameValueCollection values, Type type)
        {
            return JsonSerializer.Deserialize(values, type);
        }

        public virtual object Serialize(object value)
        {
            return JsonSerializer.Serializable(value);
        }

        public virtual object DeserializeInputModel(Type type, HttpMethodAttribute method)
        {
            if (this.IsPostBack)
            {
                return this.Deserialize(this.Form, type);
            }
            else
            {
                return this.Deserialize(this.QueryString, type);
            }
        }

        public virtual object Deserialize(string buffer, Type type)
        {
            if ( string.IsNullOrEmpty( buffer ) )
            {
                return null;
            }
            buffer = buffer.TrimStart().TrimEnd();
            if ( string.IsNullOrEmpty( buffer ) )
            {
                return null;
            }
            return JsonSerializer.Deserialize(buffer, type);
        }

        public virtual T Deserialize<T>(string buffer)
        {
            object value = JsonSerializer.Deserialize(buffer, typeof(T));
            if (value == null)
            {
                return default(T);
            }
            return (T)value;
        }

        protected HttpResponse Response
        {
            get
            {
                HttpContext context = HttpContext.Current;
                return context.Response;
            }
        }

        protected HttpRequest Request
        {
            get
            {
                HttpContext context = HttpContext.Current;
                return context.Request;
            }
        }

        public int StateCode
        {
            get
            {
                return this.Response.StatusCode;
            }
            set
            {
                this.Response.StatusCode = value;
            }
        }

        public bool IsPostBack
        {
            get
            {
                return this.HttpMethod.IsPostBack();
            }
        }

        public string HttpMethod
        {
            get
            {
                return this.Request.HttpMethod;
            }
        }

        public HttpFileCollection Files
        {
            get
            {
                return this.Request.Files;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                return this.Request.Form;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return this.Request.QueryString;
            }
        }

        public virtual void Write(byte[] buffer)
        {
            this.Response.BinaryWrite(buffer);
        }

        public virtual void Write(string s)
        {
            this.Response.Write(s);
        }

        public virtual void Write(char[] buffer, int index, int count)
        {
            this.Response.Write(buffer, index, count);
        }

        public virtual void Write(char[] buffer)
        {
            this.Response.Write(buffer, 0, buffer.Length);
        }

        public virtual void Write(object o)
        {
            object value = this.Serialize(o);
            if (value != null)
            {
                if (value is string)
                    this.Write((string)value);
                else
                    this.Write((byte[])value);
            }
        }
    }
}
