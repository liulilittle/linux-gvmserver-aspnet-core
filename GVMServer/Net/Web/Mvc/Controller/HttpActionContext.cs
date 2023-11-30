namespace GVMServer.Net.Web.Mvc.Controller
{
    using System;

    internal class HttpActionContext
    {
        public Controller Controller
        {
            get;
            set;
        }

        public HttpMethodAttribute Method
        {
            get;
            set;
        }

        public Type ModelType
        {
            get;
            set;
        }

        public object DirectAction
        {
            get;
            set;
        }

        public object InputAction
        {
            get;
            set;
        }

        public Type ActionReturnType
        {
            get;
            set;
        }

        public object Invoke(Func<object> model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            object result = null;
            if (this.InputAction != null)
            {
                if (this.ActionReturnType == typeof(void))
                    ((Action<object>)this.InputAction)(model());
                else
                    result = ((Func<object, object>)this.InputAction)(model());
            }
            else
            {
                if (this.ActionReturnType == typeof(void))
                    ((Action)this.DirectAction)();
                else
                    result = ((Func<object>)this.DirectAction)();
            }
            return result;
        }
    }
}
