namespace GVMServer.DDD.Hub
{
    using GVMServer.Diagnostics;
    using GVMServer.Utilities;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;

    public static class HubContainer
    {
        /// <summary>
        /// IList<Binding>
        /// </summary>
        private static IDictionary<int, IDictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>>> 
            m_bindings = new Dictionary<int, IDictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>>>(300);

        public static void Dispose()
        {
            m_bindings?.Clear();
            m_bindings = null;
        }

        /// <summary>
        /// 从程序集中加载集线器
        /// </summary>
        /// <param name="assembly">包含集线器的程序集</param>
        public static void Load(Assembly assembly)
        {
            Contract.Requires<ArgumentNullException>(assembly != null, "assembly");
            lock (m_bindings)
            {
                foreach (Type clazz in assembly.GetExportedTypes())
                {
                    if (!clazz.IsInterface && System.Linq.Enumerable.FirstOrDefault(clazz.GetInterfaces(), (i) => i.GUID == typeof(IHub<,>).GUID) != null)
                    {
                        HubAttribute attr = Attributes.GetAttribute<HubAttribute>(clazz);
                        if (attr != null)
                        {
                            ConstructorInfo ctor = clazz.GetConstructor(Type.EmptyTypes);
                            object hub = ctor.Invoke(null);

                            IDictionary<int, IDictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>>> a = m_bindings;
                            IDictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>> b;
                            IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>> c;
                            IDictionary<string, IDictionary<string, IDictionary<string, object>>> d;
                            IDictionary<string, IDictionary<string, object>> e;
                            IDictionary<string, object> f;

                            if (!a.TryGetValue(attr.Condition1, out b))
                            {
                                b = new Dictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>>();
                                a.Add(attr.Condition1, b);
                            }
                            if (!b.TryGetValue(attr.Condition2, out c))
                            {
                                c = new Dictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>();
                                b.Add(attr.Condition2, c);
                            }
                            if (!c.TryGetValue(attr.Condition3, out d))
                            {
                                d = new Dictionary<string, IDictionary<string, IDictionary<string, object>>>();
                                c.Add(attr.Condition3, d);
                            }
                            if (!d.TryGetValue(attr.Condition4 ?? string.Empty, out e))
                            {
                                e = new Dictionary<string, IDictionary<string, object>>();
                                d.Add(attr.Condition4 ?? string.Empty, e);
                            }
                            if (!e.TryGetValue(attr.Condition5 ?? string.Empty, out f))
                            {
                                f = new Dictionary<string, object>();
                                e.Add(attr.Condition5 ?? string.Empty, f);
                            }
                            object hh;
                            if (f.TryGetValue(attr.Condition6 ?? string.Empty, out hh))
                            {
                                throw new ArgumentException(string.Format("hub: key {0} rep {1}", clazz.FullName, hh.GetType().FullName));
                            }
                            else
                            {
                                f.Add(attr.Condition6 ?? string.Empty, hub);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取一个有效的集线器
        /// </summary>
        /// <param name="condition1">条件一</param>
        /// <param name="condition2">条件二</param>
        /// <param name="condition3">条件三</param>
        /// <param name="condition4">条件四</param>
        /// <param name="condition5">条件五</param>
        /// <param name="condition6">条件六</param>
        /// <returns></returns>
        public static IHub<InT, OutT> Get<InT, OutT>(int condition1, int condition2, int condition3 = 0, string condition4 = null,
            string condition5 = null, string condition6 = null)
        {
            lock (m_bindings)
            {
                condition4 = condition4 ?? string.Empty;
                condition5 = condition5 ?? string.Empty;
                condition6 = condition6 ?? string.Empty;

                IDictionary<int, IDictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>>> a = m_bindings;
                IDictionary<int, IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>>> b;
                IDictionary<int, IDictionary<string, IDictionary<string, IDictionary<string, object>>>> c;
                IDictionary<string, IDictionary<string, IDictionary<string, object>>> d;
                IDictionary<string, IDictionary<string, object>> e;
                IDictionary<string, object> f;

                IHub<InT, OutT> hub = null;
                object obj = null;
                if (a.TryGetValue(condition1, out b) && b.TryGetValue(condition2, out c) && c.TryGetValue(condition3, out d) &&
                    d.TryGetValue(condition4, out e) && e.TryGetValue(condition5, out f) && f.TryGetValue(condition6, out obj))
                {
                    hub = obj as IHub<InT, OutT>;
                }
                return hub;
            }
        }
    }
}
