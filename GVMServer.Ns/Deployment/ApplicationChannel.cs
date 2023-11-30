namespace GVMServer.Ns.Deployment
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using GVMServer.Linq;
    using GVMServer.Ns.Deployment.Basic;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Model;
    using GVMServer.Ns.Net.Mvh;
    using Ns = GVMServer.Ns.Functional.Ns;

    public class ApplicationChannel : ISocketChannelManagement
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly static ISocket[] m_aoEmptySockets = new ISocket[0];

        private class ApplicationChannelTable
        {
            public readonly ConcurrentDictionary<Guid, NodeChannel> m_poNodelChannelTable = new ConcurrentDictionary<Guid, NodeChannel>();
        }

        private readonly ConcurrentDictionary<ApplicationType, ApplicationChannelTable> m_poApplicationChannelTable
            = new ConcurrentDictionary<ApplicationType, ApplicationChannelTable>();
        public readonly ConcurrentDictionary<NsClient, ConcurrentDictionary<ISocket, ISocket>> m_poNodePhysicsTunnelSocket
            = new ConcurrentDictionary<NsClient, ConcurrentDictionary<ISocket, ISocket>>();
        private readonly ApplicationChannelMapping m_poApplicationChannelMapping = new ApplicationChannelMapping();
        private readonly EventHandler<Message> m_pfnOnMessageHandler;
        private readonly EventHandler<EventArgs> m_pfnOnOpenHandler;
        private readonly EventHandler<EventArgs> m_pfnOnAbortHandler;
        private readonly Action<object> m_pfnRetransmissionTickAlways;
        private readonly object m_syncobj = new object();

        private sealed class NodeChannel
        {
            public bool Available => this.Socket?.Available ?? false;

            public ISocket Socket { get; set; }

            public string Address { get; set; }

            public void Close()
            {
                var socket = this.Socket;
                if (socket != null)
                {
                    socket.Close();
                }

                if (socket is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        public ServerApplication ServerApplication { get; }

        public int AvailableChannels { get; private set; }

        public SocketMvhHandlerContainer HandlerContainer { get; } = new SocketMvhHandlerContainer();

        public SocketMvhRetransmission Retransmission { get; }

        public SocketMvhMessageQueue MessageQueue { get; }

        public ApplicationChannel(ServerApplication serverApplication, int maxRetransmissionConcurrent)
        {
            this.ServerApplication = serverApplication ?? throw new ArgumentNullException(nameof(serverApplication));
            this.Retransmission = new SocketMvhRetransmission(this);
            this.MessageQueue = new SocketMvhMessageQueue(this, Environment.ProcessorCount);

            this.m_pfnRetransmissionTickAlways = (state) =>
            {
                SocketMvhRetransmission retransmission = this.Retransmission;
                retransmission.DoEvents(maxRetransmissionConcurrent);
            };
            this.m_pfnOnMessageHandler = (sender, e) => OnProcessMessage((ISocket)sender, e);
            this.m_pfnOnOpenHandler = (sender, e) =>
            {
                lock (this.m_syncobj)
                {
                    this.AvailableChannels++;
                }
                if (sender is NsClient socket)
                {
                    this.OnProcessEstablishChannel(socket);
                }
            };
            this.m_pfnOnAbortHandler = (sender, e) =>
            {
                lock (this.m_syncobj)
                {
                    this.AvailableChannels--;
                }
                if (sender is NsClient poSocket)
                {
                    this.CloseAllVirtualChannel(poSocket);
                    this.OnProcessAbortChannel(poSocket);
                }
                else if (sender is ApplicationVirtualSocket poVirtualChannel)
                {
                    NsClient poPhysicalChannel = (NsClient)poVirtualChannel.GetPhysicalChannel();
                    this.CloseVirtualChannel(poPhysicalChannel, poVirtualChannel.ApplicationType, poVirtualChannel.Id);
                }
            };
            this.MessageQueue.AddTickAlways(this.m_pfnRetransmissionTickAlways, this);
        }

        private ApplicationChannelTable GetApplicationChannelTable(ApplicationType applicationType)
        {
            ApplicationChannelTable poApplicationChannelTable = null;
            lock (m_poApplicationChannelTable)
            {
                if (!m_poApplicationChannelTable.TryGetValue(applicationType, out poApplicationChannelTable) || poApplicationChannelTable == null)
                {
                    m_poApplicationChannelTable[applicationType]
                        = poApplicationChannelTable
                        = new ApplicationChannelTable();
                }
            }
            return poApplicationChannelTable;
        }

        public virtual bool AddChannel(ApplicationType applicationType, Guid id, ApplicationType authenticationType, Guid authenticationId, string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            ApplicationChannelTable poApplicationChannelTable = GetApplicationChannelTable(applicationType);
            if (poApplicationChannelTable == null)
            {
                return false;
            }

            lock (poApplicationChannelTable)
            {
                poApplicationChannelTable.m_poNodelChannelTable.TryGetValue(id, out NodeChannel poChannel);
                if (poChannel != null)
                {
                    if (!poChannel.Available ||
                        address != poChannel.Address) // 地址不同时中断通道重新建立新的地址（一般只可能出现在对应节点服务器被移动到新的服务器地址）
                    {
                        poChannel.Close();
                    }
                    else
                    {
                        return true;
                    }
                }

                var socket = this.CreateChannel(applicationType, id, authenticationType, authenticationId, address);
                poChannel = new NodeChannel()
                {
                    Socket = socket,
                    Address = address,
                };
                socket.OnAbort += m_pfnOnAbortHandler;
                socket.OnMessage += m_pfnOnMessageHandler;
                socket.OnOpen += m_pfnOnOpenHandler;

                socket.Run();
                poApplicationChannelTable.m_poNodelChannelTable[id] = poChannel;
            }
            return true;
        }

        protected virtual NsClient CreateChannel(ApplicationType applicationType, Guid id, ApplicationType authenticationType, Guid authenticationId, string address)
        {
            ISocketHandler handler = this.GetSocketHandler();
            return new NsClient(handler, applicationType, id, authenticationType, authenticationId, address, ServerApplication.MaxChannelConcurrent);
        }

        protected virtual ISocketHandler GetSocketHandler()
        {
            return this.ServerApplication.GetSocketHandler();
        }

        private ConcurrentDictionary<ISocket, ISocket> CloseVirtualChannel(NsClient sender, ApplicationType applicationType, Guid id)
        {
            ConcurrentDictionary<ISocket, ISocket> poPhysicsTunnelSocketSet = null;
            if (sender == null)
            {
                return poPhysicsTunnelSocketSet;
            }
            NodeChannel poNodeChannel = null;
            lock (m_poNodePhysicsTunnelSocket)
            {
                ApplicationChannelTable poApplicationChannelTable = GetApplicationChannelTable(applicationType);
                lock (poApplicationChannelTable)
                {
                    if (poApplicationChannelTable.m_poNodelChannelTable.TryRemove(id, out poNodeChannel) &&
                        m_poNodePhysicsTunnelSocket.TryGetValue(sender, out poPhysicsTunnelSocketSet))
                    {
                        if (poNodeChannel.Socket != null)
                        {
                            poPhysicsTunnelSocketSet.TryRemove(poNodeChannel.Socket, out ISocket poVirSocket);
                        }

                        if (poNodeChannel.Socket is ApplicationVirtualSocket poVirtualSocket)
                        {
                            var credentials = poVirtualSocket.Credentials;
                            if (credentials != null)
                            {
                                m_poApplicationChannelMapping.GetMappingObject(applicationType)?.Remove(credentials.PlatformName, credentials.ServerNo);
                            }
                        }
                    }
                }
            }
            if (poNodeChannel != null)
            {
                poNodeChannel.Close();
            }
            return poPhysicsTunnelSocketSet;
        }

        private void CloseAllVirtualChannel(NsClient sender)
        {
            if (sender == null)
            {
                return;
            }

            ConcurrentDictionary<ISocket, ISocket> poPhysicsTunnelSocketSet = null;
            lock (m_poNodePhysicsTunnelSocket)
            {
                if (!m_poNodePhysicsTunnelSocket.
                    TryRemove(sender, out poPhysicsTunnelSocketSet) || poPhysicsTunnelSocketSet == null)
                {
                    return;
                }
            }

            foreach (ISocket socket in poPhysicsTunnelSocketSet.Keys)
            {
                socket?.Close();
            }
        }

        protected virtual bool AddVirtualChannel(NsClient channel, AcceptSocketMessage message)
        {
            if (channel == null || message == null)
            {
                return false;
            }

            var credentials = message.Credentials;
            if (credentials == null)
            {
                return false;
            }

            ApplicationVirtualSocket poVirtualChannel = null;
            ApplicationChannelTable poApplicationChannelTable = GetApplicationChannelTable(message.ApplicationType);
            if (poApplicationChannelTable == null)
            {
                return false;
            }

            lock (m_poNodePhysicsTunnelSocket)
            {
                lock (poApplicationChannelTable)
                {
                    var poPhysicsTunnelSocketSet = CloseVirtualChannel(channel, message.ApplicationType, message.Id);

                    // 建立虚拟的套接字通道
                    poVirtualChannel = CreateVirtualSocket(channel, message.ApplicationType,
                        message.Id, credentials);
                    if (poVirtualChannel == null)
                    {
                        return false;
                    }

                    // 绑定虚拟通道的必要信息
                    NodeChannel poNodeChannel = new NodeChannel
                    {
                        Address = string.Empty,
                        Socket = poVirtualChannel
                    };

                    poVirtualChannel.Abort += m_pfnOnAbortHandler;
                    poVirtualChannel.Message += m_pfnOnMessageHandler;
                    poVirtualChannel.Accept += m_pfnOnOpenHandler;

                    poApplicationChannelTable.m_poNodelChannelTable[message.Id] = poNodeChannel;
                    if (poPhysicsTunnelSocketSet == null)
                    {
                        poPhysicsTunnelSocketSet = new ConcurrentDictionary<ISocket, ISocket>();
                        m_poNodePhysicsTunnelSocket[channel] = poPhysicsTunnelSocketSet;
                    }
                    poPhysicsTunnelSocketSet.TryAdd(poVirtualChannel, poVirtualChannel);

                    m_poApplicationChannelMapping.GetMappingObject(message.ApplicationType)?.Add(credentials.PlatformName, credentials.ServerNo, poVirtualChannel);
                }
            }
            if (poVirtualChannel != null)
            {
                poVirtualChannel.OnAccept(EventArgs.Empty);
            }
            return true;
        }

        protected virtual void OnProcessMessage(ISocket socket, Message message)
        {
            if (message.CommandId == Commands.Commands_Transitroute) // 中继路由命令它只允许stage组件使用此命令。
            {
                TransitrouteMessage poTransitrouteMessage = TransitrouteMessage.From(message.Payload);
                if (poTransitrouteMessage == null)
                {
                    return;
                }

                ISocket poChannel = GetChannel(poTransitrouteMessage.ApplicationType, poTransitrouteMessage.Id);
                if (poChannel == null)
                {
                    return;
                }

                if (poChannel is ApplicationVirtualSocket poVirtualChannel)
                {
                    poVirtualChannel.OnMessage(poTransitrouteMessage.Message);
                }
                else
                {
                    this.MessageQueue.AddMessage(poChannel, poTransitrouteMessage.Message);
                }
            }
            else if (message.CommandId == Commands.Commands_AcceptSocket) // 接受套接字通道
            {
                AcceptSocketMessage poAcceptSocketMessage = Message.Deserialize<AcceptSocketMessage>(message);
                if (poAcceptSocketMessage == null)
                {
                    return;
                }

                if (socket is NsClient poChannel)
                {
                    this.AddVirtualChannel(poChannel, poAcceptSocketMessage);
                }
            }
            else if (message.CommandId == Commands.Commands_AbortSocket) // 中断套接字通道
            {
                AbortSocketMessage poAbortSocketMessage = Message.Deserialize<AbortSocketMessage>(message);
                if (poAbortSocketMessage == null)
                {
                    return;
                }

                // 中止虚拟的套接字通道
                ISocket poChannel = GetChannel(poAbortSocketMessage.ApplicationType, poAbortSocketMessage.Id);
                if (poChannel == null)
                {
                    return;
                }

                poChannel.Close();
            }
            else if (message.CommandId == Commands.Commands_AcceptAllSocket)
            {
                AcceptAllSocketMessage poAcceptAllSocketMessage = Message.Deserialize<AcceptAllSocketMessage>(message);
                if (poAcceptAllSocketMessage == null || poAcceptAllSocketMessage.AcceptSockets.IsNullOrEmpty())
                {
                    return;
                }

                if (socket is NsClient poChannel)
                {
                    lock (m_poNodePhysicsTunnelSocket)
                    {
                        foreach (AcceptSocketMessage poAcceptSocketMessage in poAcceptAllSocketMessage.AcceptSockets)
                        {
                            if (poAcceptSocketMessage == null)
                            {
                                continue;
                            }

                            this.AddVirtualChannel(poChannel, poAcceptSocketMessage);
                        }
                    }
                }
            }
            else if (message.CommandId == Commands.Commands_RoutingMessage)
            {
                RoutingMessage poRoutingMessage = RoutingMessage.From(message.Payload);
                if (poRoutingMessage == null)
                {
                    return;
                }

                ISocket poOppositeSocket = GetChannel(poRoutingMessage.PeerApplicationType, poRoutingMessage.PeerPlatform, poRoutingMessage.PeekServerNo);
                if (poOppositeSocket == null)
                {
                    return;
                }

                poOppositeSocket.Send(message);
            }
            else
            {
                this.MessageQueue.AddMessage(socket, message);
            }
        }

        protected virtual ApplicationVirtualSocket CreateVirtualSocket(ISocket physicalChannel, ApplicationType applicationType, Guid id, Ns credentials)
        {
            return new ApplicationVirtualSocket(this, physicalChannel, applicationType, id, credentials);
        }

        protected virtual void OnProcessEstablishChannel(NsClient socket)
        {

        }

        protected virtual void OnProcessAbortChannel(NsClient socket)
        {

        }

        protected virtual SocketMvhContext CreateContext(ISocket socket, Message message)
        {
            return new SocketMvhContext(this, socket, message);
        }

        public virtual ISocket CloseChannel(ApplicationType applicationType, Guid guid)
        {
            ApplicationChannelTable poApplicationChannelTable = GetApplicationChannelTable(applicationType);
            if (poApplicationChannelTable == null)
            {
                return null;
            }

            NodeChannel poNodeChannel = null;
            lock (poApplicationChannelTable)
            {
                if (!poApplicationChannelTable.m_poNodelChannelTable.TryRemove(guid, out poNodeChannel))
                {
                    poNodeChannel = null;
                }
            }

            if (poNodeChannel == null)
            {
                return null;
            }

            poNodeChannel.Close();
            return poNodeChannel.Socket;
        }

        public virtual ISocket GetChannel(ApplicationType applicationType, Guid guid)
        {
            ApplicationChannelTable poApplicationChannelTable = GetApplicationChannelTable(applicationType);
            if (poApplicationChannelTable == null)
            {
                return null;
            }

            NodeChannel poNodeChannel = null;
            lock (poApplicationChannelTable)
            {
                if (!poApplicationChannelTable.
                    m_poNodelChannelTable.TryGetValue(guid, out poNodeChannel))
                {
                    poNodeChannel = null;
                }
            }

            if (poNodeChannel == null)
            {
                return null;
            }

            return poNodeChannel?.Socket;
        }

        public virtual IEnumerator<ApplicationType> GetEnumerator()
        {
            return m_poApplicationChannelTable.Keys.GetEnumerator();
        }

        public virtual IEnumerable<ISocket> GetAllSockets(ApplicationType applicationType)
        {
            ApplicationChannelTable poApplicationChannelTable = GetApplicationChannelTable(applicationType);
            if (poApplicationChannelTable == null)
            {
                return m_aoEmptySockets;
            }

            poApplicationChannelTable.m_poNodelChannelTable.Values.GetEnumerator();
            return poApplicationChannelTable.m_poNodelChannelTable.Values.Conversion((poChannel) => poChannel?.Socket);
        }

        public virtual IEnumerable<string> GetAllPlatformNames(ApplicationType applicationType) => m_poApplicationChannelMapping.GetMappingObject(applicationType)?.GetAllPlatformNames();

        public virtual IEnumerable<KeyValuePair<int, ISocket>> GetAllSockets(ApplicationType applicationType, string platform) => m_poApplicationChannelMapping.GetMappingObject(applicationType)?.GetAllSockets(platform);

        public virtual ISocket GetChannel(ApplicationType applicationType, string platform, int sid) => m_poApplicationChannelMapping.GetMappingObject(applicationType)?.GetChannel(platform, sid);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public virtual void ProcessMessage(ISocket socket, Message message)
        {
            if (!this.Retransmission.AckRetransmission(applicationType: socket.ApplicationType, id: socket.Id,
                commands: message.CommandId, ackNo: message.SequenceNo, message: message))
            {
                ISocketMvhHandler handler = this.HandlerContainer.GetHandler(applicationType: socket.ApplicationType, commandId: message.CommandId);
                if (handler != null)
                {
                    SocketMvhContext context = CreateContext(socket, message);
                    handler.ProcessRequest(context);
                }
            }
        }

        public virtual IEnumerable<ApplicationType> GetAllApplicationType()
        {
            return this.m_poApplicationChannelTable.Keys;
        }
    }
}
