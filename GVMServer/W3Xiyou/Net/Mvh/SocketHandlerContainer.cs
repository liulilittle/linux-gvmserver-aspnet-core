namespace GVMServer.W3Xiyou.Net.Mvh
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using GVMServer.Utilities;

    public class SocketHandlerContainer
    {
        private readonly IDictionary<ushort, HandlerObject> handlers = new Dictionary<ushort, HandlerObject>();

        public class HandlerObject
        {
            public SocketHandlerAttribute Attribute { get; set; }

            public SocketHandler Handler { get; set; }
        }

        private void AddOrUpdate(ConstructorInfo constructor, SocketHandlerAttribute attribute)
        {
            if (!this.handlers.ContainsKey(attribute.CommandId))
            {
                SocketHandler handler = constructor.Invoke(null) as SocketHandler;
                this.handlers.Add(attribute.CommandId, new HandlerObject() { Attribute = attribute, Handler = handler });
            }
        }

        public HandlerObject GetHandler(ushort commandId)
        {
            this.handlers.TryGetValue(commandId, out HandlerObject h);
            return h;
        }

        public void Load(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            foreach (Type clazz in assembly.GetExportedTypes())
            {
                if (!typeof(SocketHandler).IsAssignableFrom(clazz))
                {
                    continue;
                }
                ConstructorInfo ctor = clazz.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                {
                    continue;
                }
                SocketHandlerAttribute handlerAttribute = Attributes.GetAttribute<SocketHandlerAttribute>(clazz);
                if (handlerAttribute == null)
                {
                    continue;
                }
                this.AddOrUpdate(ctor, handlerAttribute);
            }
        }
    }
}
