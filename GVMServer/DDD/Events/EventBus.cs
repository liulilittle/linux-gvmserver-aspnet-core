namespace GVMServer.DDD.Events
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using GVMServer.Collection;

    /// <summary>
    /// 领域事件总线
    /// </summary>
    public class EventBus : IDisposable
    {
        private ConcurrentDictionary<Type, IList<Binding>> m_events =
            new ConcurrentDictionary<Type, IList<Binding>>(100, 300);
        private MethodInfo m_subscribet = typeof(EventBus).GetMethods().
            First(i => i.IsGenericMethod && i.Name == "Subscribe");
        private volatile bool m_disposed = false;

        private class Binding
        {
            public object sender;
            public Type declared;
            public Type eventtype;
            public Action<IEvent, Action<object>> handler;
        }

        private static readonly EventBus EVENT_BUS = new EventBus();

        public static EventBus Current
        {
            get
            {
                return EVENT_BUS;
            }
        }

        /// <summary>
        /// 订阅事件处理程序
        /// </summary>
        /// <typeparam name="TEvent">事件参数标识</typeparam>
        /// <param name="handler">事件处理程序</param>
        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            Type key = typeof(TEvent);

            IList<Binding> bindings = null;
            if (!m_events.TryGetValue(key, out bindings))
            {
                bindings = new SafetyList<Binding>();
                m_events.TryAdd(key, bindings);
            }
            Type clazz = handler.GetType();
            MethodInfo mi = clazz.GetMethod("Handle");

            Binding binding = new Binding();
            binding.sender = handler;
            binding.declared = clazz;
            binding.eventtype = key;
            binding.handler = (Action<IEvent, Action<object>>)Activator.
                CreateInstance(typeof(Action<IEvent, Action<object>>),
                handler,
                mi.MethodHandle.GetFunctionPointer());
            bindings.Add(binding);
        }

        public void Subscribe(Assembly[] assemblys)
        {
            if (assemblys == null || assemblys.Length <= 0)
            {
                throw new ArgumentNullException();
            }
            foreach (Assembly assembly in assemblys)
            {
                Subscribe(assembly);
            }
        }

        public void Subscribe(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException();
            }
            foreach (Type type in assembly.GetExportedTypes())
            {
                if (GetInterface(type) == null)
                    continue;
                ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    continue;
                Subscribe(ctor.Invoke(null));
            }
        }

        private Type GetInterface(Type type)
        {
            return type.GetInterface(typeof(IEventHandler<>).FullName);
        }

        public void Subscribe(object handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException();
            }
            m_subscribet.
                MakeGenericMethod(new[]
                {
                    GetInterface(
                        handler.
                        GetType()).
                    GetGenericArguments()[0]
                }).Invoke(this,
                new[]
                {
                    handler
                });
        }

        /// <summary>
        /// 发布用户事件源
        /// </summary>
        /// <typeparam name="T">事件处理器回调参数类型</typeparam>
        /// <param name="e">事件对象</param>
        /// <param name="callback">回调函数</param>
        public void Publish<T>(IEvent e, Action<T> callback) where T : class
        {
            Publish(e, (r) =>
            {
                if (callback != null)
                {
                    callback(r as T);
                }
            });
        }

        public void Publish(IEvent e)
        {
            Publish(e, null);
        }

        public bool Publish(IEvent e, Action<object> callback)
        {
            if (e == null)
            {
                return false;
            }
            IList<Binding> bindings = null;
            Type clazz = e.GetType();
            if (!m_events.TryGetValue(clazz, out bindings))
            {
                return false;
            }
            foreach (Binding binding in bindings)
            {
                if (binding == null)
                    continue;
                Action<IEvent, Action<object>> handler = binding.handler;
                handler(e, callback);
            }
            return true;
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!m_disposed)
                {
                    m_disposed = true;

                    m_events.Clear();
                    m_events = null;
                    m_subscribet = null;
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
