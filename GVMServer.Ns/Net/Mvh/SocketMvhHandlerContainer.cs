using static GVMServer.Ns.Net.Mvh.SocketMvhHandlerContainer;

namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using GVMServer.Utilities;
    using Commands = GVMServer.Ns.Enum.Commands;
    using SocketHandlerTable = System.Collections.Concurrent.ConcurrentDictionary<GVMServer.Ns.Enum.Commands, HandlerObject>;

    public class SocketMvhHandlerContainer : IEnumerable<SocketHandlerTable>
    {
        public class HandlerObject
        {
            public ISocketMvhHandler SocketHandler { get; internal set; }

            public SocketMvhHandlerAttribute HandlerAttribute { get; internal set; }
        }

        private readonly ConcurrentDictionary<ApplicationType, SocketHandlerTable> m_poHandlerTable =
            new ConcurrentDictionary<ApplicationType, SocketHandlerTable>();

        private SocketHandlerTable GetTable(ApplicationType applicationType, bool allowAdditions = false)
        {
            ConcurrentDictionary<Commands, HandlerObject> poTable = null;
            lock (this.m_poHandlerTable)
            {
                this.m_poHandlerTable.TryGetValue(applicationType, out poTable);
                if (allowAdditions && poTable == null)
                {
                    this.m_poHandlerTable[applicationType]
                        = poTable
                        = new ConcurrentDictionary<Commands, HandlerObject>();
                }
            }
            return poTable;
        }

        protected virtual void AddOrUpdate(ConstructorInfo constructor, SocketMvhHandlerAttribute attribute)
        {
            if (attribute == null || constructor == null)
            {
                return;
            }

            foreach (ApplicationType applicationType in attribute.ApplicationType ?? SocketMvhHandlerAttribute.DefaultApplicationType)
            {
                var poTable = GetTable(applicationType, true);
                lock (poTable)
                {
                    if (!poTable.ContainsKey(attribute.CommandId))
                    {
                        ISocketMvhHandler handler = constructor.Invoke(null) as ISocketMvhHandler;
                        if (handler == null)
                        {
                            throw new SystemException("An unexpected problem occurred while building the ISocketMvhHandler instance");
                        }
                        poTable.TryAdd(attribute.CommandId, new HandlerObject() { HandlerAttribute = attribute, SocketHandler = handler });
                    }
                }
            }
        }

        public virtual void Load(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            foreach (Type clazz in assembly.GetTypes())
            {
                if (!typeof(ISocketMvhHandler).IsAssignableFrom(clazz))
                {
                    continue;
                }
                ConstructorInfo ctor = clazz.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                {
                    continue;
                }
                SocketMvhHandlerAttribute handlerAttribute = Attributes.GetAttribute<SocketMvhHandlerAttribute>(clazz);
                if (handlerAttribute == null)
                {
                    continue;
                }
                this.AddOrUpdate(ctor, handlerAttribute);
            }
        }

        public int Count => this.m_poHandlerTable.Count;

        public virtual void Clear()
        {
            this.m_poHandlerTable.Clear();
        }

        public virtual SocketMvhHandlerAttribute GetAttribute(ApplicationType applicationType, Commands commandId)
        {
            HandlerObject pho = null;
            GetTable(applicationType)?.TryGetValue(commandId, out pho);
            return pho?.HandlerAttribute;
        }

        public virtual ISocketMvhHandler GetHandler(ApplicationType applicationType, Commands commandId)
        {
            HandlerObject pho = null;
            GetTable(applicationType)?.TryGetValue(commandId, out pho);
            return pho?.SocketHandler;
        }

        public virtual SocketMvhHandlerAttribute GetHandlerAttribute(ApplicationType applicationType, Commands commandId)
        {
            HandlerObject pho = null;
            GetTable(applicationType)?.TryGetValue(commandId, out pho);
            return pho?.HandlerAttribute;
        }

        public virtual IEnumerator<SocketHandlerTable> GetEnumerator()
        {
            return this.m_poHandlerTable.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
