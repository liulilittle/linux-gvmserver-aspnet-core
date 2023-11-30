namespace GVMServer.DDD.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class IoC<TObject>
        where TObject : class
    {
        private readonly ISet<object> g_persistent = null;
        private readonly IDictionary<Type, object> g_relational = null;

        public IoC()
        {
            g_persistent = new HashSet<object>();
            g_relational = new Dictionary<Type, object>();
        }

        public static bool Invalid(Type clazz)
        {
            if (clazz == null || clazz.IsValueType)
            {
                return false;
            }
            return !typeof(IServiceLocator).IsAssignableFrom(clazz);
        }

        private object FindObject(Type type)
        {
            lock (this)
            {
                object persistent = null;
                if (type == null)
                    return null;
                if (g_relational.TryGetValue(type, out persistent))
                    return persistent;
                persistent = g_persistent.FirstOrDefault(obj => type.IsInstanceOfType(obj));
                if (persistent != null)
                    g_relational.Add(type, persistent);
                return persistent;
            }
        }

        private Type FindInheritedType(Type type, Assembly[] assemblys)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                Type inherited = assemblys.Select(i => i.GetTypes().FirstOrDefault(j => j.IsSubclassOf(type))).
        FirstOrDefault(i => i != null);
                return inherited;
            }
            return type;
        }

        private object CreateObject(Type type, Assembly[] assemblys)
        {
            lock (this)
            {
                ConstructorInfo ctor = type.GetConstructors().FirstOrDefault();
                if (ctor == null)
                {
                    return null;
                }
                ParameterInfo[] parameters = ctor.GetParameters();
                if (!parameters.Any())
                {
                    object obj = ctor.Invoke(null);
                    g_persistent.Add(obj);
                    return obj;
                }
                else
                {
                    object[] args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo parameter = parameters[i];

                        Type inherited = FindInheritedType(parameter.ParameterType, assemblys);
                        if (inherited == null)
                        {
                            inherited = parameter.ParameterType;
                        }
                        object push = FindObject(inherited);
                        if (push == null)
                        {
                            push = CreateObject(inherited, assemblys);
                        }
                        if (push == null)
                        {
                            return null;
                        }
                        args[i] = push;
                    }
                    object obj = ctor.Invoke(args);
                    g_persistent.Add(obj);
                    return obj;
                }
            }
        }

        public virtual bool Register(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException();
            lock (this)
            {
                return g_persistent.Add(obj);
            }
        }

        public virtual bool Register(Type type, Assembly[] assemblys)
        {
            return Resolve(type, assemblys) != null;
        }

        public virtual bool Register(Type type)
        {
            if (type == null)
                throw new ArgumentNullException();
            return Register(type, new[] { type.Assembly });
        }

        public virtual bool Register<T>()
        {
            return Register(typeof(T));
        }

        public virtual bool Register<T>(Assembly[] assemblys)
        {
            return Register(typeof(T), assemblys);
        }

        public virtual object Get(Type type)
        {
            if (type == null)
                throw new ArgumentNullException();
            return FindObject(type);
        }

        public virtual T Get<T>()
        {
            object obj = Get(typeof(T));
            if (obj == null)
                return default(T);
            return (T)obj;
        }

        protected virtual object Resolve(Type type)
        {
            if (type == null)
                throw new ArgumentNullException();
            return Resolve(type, new[] { type.Assembly });
        }

        private void CheckAssmeblyAndThrowEmpty(Assembly[] assemblys)
        {
            if (assemblys == null)
                throw new ArgumentNullException();
            if (assemblys.Length <= 0)
                throw new ArgumentException();
            if (assemblys.Where(i => i == null).Any())
                throw new ArgumentNullException();
        }

        public virtual void Load(Assembly assembly)
        {
            Load(new[] { assembly });
        }

        public virtual void Load(Assembly[] assemblys)
        {
            lock (this)
            {
                CheckAssmeblyAndThrowEmpty(assemblys);
                foreach (Assembly assembly in assemblys)
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (!typeof(TObject).IsAssignableFrom(type))
                            continue;
                        Resolve(type, assemblys);
                    }
                }
            }
        }

        protected virtual object Resolve(Type type, Assembly[] assemblys)
        {
            CheckAssmeblyAndThrowEmpty(assemblys);
            if (type == null)
                throw new ArgumentNullException();
            if (!typeof(TObject).IsAssignableFrom(type))
                throw new ArgumentException("type");
            lock (this)
            {
                object obj = FindObject(type);
                if (obj == null)
                {
                    obj = CreateObject(type, assemblys);
                }
                return obj;
            }
        }
    }
}
