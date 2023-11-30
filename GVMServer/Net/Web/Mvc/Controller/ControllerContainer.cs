namespace GVMServer.Net.Web.Mvc.Controller
{
    using GVMServer.Net.Web.Mvc.Utilits;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class ControllerContainer
    {
        private readonly IList<HttpActionContext> m_contexts = new List<HttpActionContext>();
        private readonly object m_look = new object();

        public void Load(Assembly assembly)
        {
            foreach (Type clazz in assembly.GetExportedTypes())
            {
                if (!typeof(Controller).IsAssignableFrom(clazz))
                    continue;
                ConstructorInfo ctor = clazz.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    continue;
                IList<Tuple<HttpMethodAttribute, MethodInfo>> methods = ResloveMethod(clazz);
                if (methods.Count > 0)
                {
                    Controller controller = (Controller)ctor.Invoke(null);
                    lock (m_look)
                    {
                        foreach (Tuple<HttpMethodAttribute, MethodInfo> tuple in methods)
                        {
                            HttpActionContext context = new HttpActionContext();
                            MethodInfo mi = tuple.Item2;
                            RuntimeMethodHandle mh = mi.MethodHandle;
                            ParameterInfo[] args = mi.GetParameters();
                            if (args.Length > 0)
                                context.ModelType = args[0].ParameterType;
                            context.ActionReturnType = mi.ReturnType;
                            if (context.ModelType == null)
                            {
                                Type dtype = (mi.ReturnType == typeof(void)) ? typeof(Action) : typeof(Func<object>);
                                context.DirectAction = dtype.GetConstructors()[0].Invoke(new object[] { controller, mh.GetFunctionPointer() });
                            }
                            else
                            {
                                Type dtype = (mi.ReturnType == typeof(void)) ? typeof(Action<object>) : typeof(Func<object, object>);
                                context.InputAction = dtype.GetConstructors()[0].Invoke(new object[] { controller, mh.GetFunctionPointer() });
                            }
                            context.Method = tuple.Item1;
                            context.Controller = controller;

                            m_contexts.Add(context);
                        }
                    }
                }
            }
        }

        private IList<Tuple<HttpMethodAttribute, MethodInfo>> ResloveMethod(Type clazz)
        {
            IList<Tuple<HttpMethodAttribute, MethodInfo>> tuples = new List<Tuple<HttpMethodAttribute, MethodInfo>>();
            foreach (MethodInfo mi in clazz.GetMethods())
            {
                HttpMethodAttribute attr = HttpMethodAttribute.Get(mi);
                if (attr == null)
                    continue;
                tuples.Add(new Tuple<HttpMethodAttribute, MethodInfo>(attr, mi));
            }
            return tuples;
        }

        internal HttpActionContext Get(string method, string path)
        {
            lock (m_look)
            {
                int len = m_contexts.Count;
                for (int i = 0; i < len; i++)
                {
                    HttpActionContext context = m_contexts[i];
                    HttpMethodAttribute attr = context.Method;
                    if (StringUnit.EqualsIgnoreCase(attr.HttpMethod, method) && UriResolveUnit.Equals(attr.MappingPathValue, path))
                        return context;
                }
                return default(HttpActionContext);
            }
        }
    }
}
