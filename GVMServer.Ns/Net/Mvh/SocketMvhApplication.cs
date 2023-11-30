namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using GVMServer.DDD.Service;
    using GVMServer.Net;
    using GVMServer.Ns.Deployment.Basic;
    using GVMServer.Ns.Net.Handler;
    using MESSAGE = GVMServer.Ns.Net.Message;
    using Ns = GVMServer.Ns.Functional.Ns;
    using SocketMvhClientTable = System.Collections.Concurrent.ConcurrentDictionary<System.Guid, SocketMvhClient>;

    public class SocketMvhApplication : IServiceBase, IEnumerable<SocketMvhClientTable>, ISocketChannelManagement
    {
        private static readonly SocketMvhClient[] m_aoEmptySocket = new SocketMvhClient[0];
        private readonly ConcurrentDictionary<ApplicationType, SocketMvhClientTable> m_poSocketTable =
            new ConcurrentDictionary<ApplicationType, SocketMvhClientTable>();
        private readonly ApplicationChannelMapping m_poApplicationChannelMapping = new ApplicationChannelMapping();
        private readonly NsListener m_poListener;
        private readonly Action<object> m_pfnRetransmissionTickAlways;

        public event EventHandler<SocketMvhClient> Open;
        public event EventHandler<SocketMvhClient> Close;
        public event EventHandler<MESSAGE> Message;

        public IServerHandler ServerHandler { get; }

        public ISocketHandler SocketHandler { get; }

        public SocketMvhHandlerContainer HandlerContainer { get; } = new SocketMvhHandlerContainer();

        public SocketMvhRetransmission Retransmission { get; }

        public SocketMvhMessageQueue MessageQueue { get; }

        public int Port { get; }

        public SocketMvhApplication(ISocketHandler handler, int port, int maxRetransmissionConcurrent)
        {
            if (port <= IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException($"Port less or equals than {IPEndPoint.MinPort} or greater than {IPEndPoint.MaxPort}");
            }

            this.SocketHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.Port = port;

            this.m_poListener = new NsListener(handler, port);
            this.m_poListener.Message += this.OnMessageHandler;
            this.m_poListener.Open += this.OnOpenHandler;
            this.m_poListener.Close += this.OnCloseHandler;

            this.Retransmission = new SocketMvhRetransmission(this);
            this.MessageQueue = new SocketMvhMessageQueue(this, Environment.ProcessorCount);
            this.m_pfnRetransmissionTickAlways = (state) =>
            {
                SocketMvhRetransmission retransmission = this.Retransmission;
                retransmission.DoEvents(maxRetransmissionConcurrent);
            };
            this.MessageQueue.AddTickAlways(this.m_pfnRetransmissionTickAlways, this);
        }

        public virtual IServerHandler GetHandler() => null;

        protected virtual ConcurrentDictionary<Guid, SocketMvhClient> GetSocketTable(ApplicationType applicationType)
        {
            lock (this.m_poSocketTable)
            {
                if (!this.m_poSocketTable.
                    TryGetValue(applicationType, out ConcurrentDictionary<Guid, SocketMvhClient> client) || client == null)
                {
                    client = new ConcurrentDictionary<Guid, SocketMvhClient>();
                    this.m_poSocketTable.TryAdd(applicationType, client);
                }
                return client;
            }
        }

        private void OnCloseHandler(object sender, EventArgs e)
        {
            NsSocket socket = (NsSocket)sender;
            if (socket.IsOpen)
            {
                bool bCloseSocket = false;
                SocketMvhClient poSocket = null;

                var oSocketTable = GetSocketTable(socket.ApplicationType);
                lock (oSocketTable)
                {
                    if (oSocketTable.TryGetValue(socket.Id, out poSocket))
                    {
                        do
                        {
                            if (poSocket != null)
                            {
                                poSocket.CloseChannel(socket);
                                if (poSocket.AvailableChannels > 0)
                                {
                                    break;
                                }
                                var credentials = poSocket.Credentials;
                                if (credentials != null)
                                {
                                    m_poApplicationChannelMapping.GetMappingObject(poSocket.ApplicationType)?.Remove(credentials.PlatformName, credentials.ServerNo);
                                }
                            }
                            bCloseSocket = oSocketTable.TryRemove(socket.Id, out SocketMvhClient x);
                        } while (false);
                    }
                }
                if (bCloseSocket && poSocket != null)
                {
                    this.OnClose(poSocket);
                }
                Console.WriteLine($"NS Abort: { socket.Id }\t{ socket.ApplicationType }");
            }
        }

        private void OnOpenHandler(object sender, EventArgs e)
        {
            NsSocket socket = (NsSocket)sender;
            do
            {
                bool bOpenSocket = false;
                SocketMvhClient poSocket = null;

                var poSocketTable = GetSocketTable(socket.ApplicationType);
                lock (poSocketTable)
                {
                    var credentials = socket.Credentials;
                    if (!poSocketTable.TryGetValue(socket.Id, out poSocket) || poSocket == null)
                    {
                        poSocket = this.CreateClient(
                            applicationType: socket.ApplicationType,
                            id: socket.Id,
                            credentials: credentials);
                        poSocketTable[socket.Id] = poSocket;
                    }
                    poSocket.AddChannel(socket);
                    if (poSocket.AvailableChannels <= 1)
                    {
                        bOpenSocket = true;
                        m_poApplicationChannelMapping.GetMappingObject(poSocket.ApplicationType)?.Add(credentials.PlatformName, credentials.ServerNo, poSocket);
                    }
                }
                if (bOpenSocket && poSocket != null)
                {
                    this.OnOpen(poSocket);
                }
                Console.WriteLine($"NS Establish: { socket.Id }\t{ socket.ApplicationType }");
            } while (false);
        }

        public virtual IEnumerable<ApplicationType> GetAllApplicationType()
        {
            return this.m_poSocketTable.Keys;
        }

        public virtual IEnumerable<ISocket> GetAllSockets(ApplicationType applicationType)
        {
            var poSocketTable = GetSocketTable(applicationType);
            if (poSocketTable == null)
            {
                return m_aoEmptySocket;
            }
            else
            {
                return poSocketTable.Values;
            }
        }

        protected virtual SocketMvhClient CreateClient(ApplicationType applicationType, Guid id, Ns credentials)
        {
            return new SocketMvhClient(this, applicationType, id, credentials);
        }

        private void OnMessageHandler(object sender, SocketMessage e)
        {
            NsSocket socket = (NsSocket)e.GetClient();
            GetSocketTable(socket.ApplicationType).TryGetValue(socket.Id, out SocketMvhClient client);
            if (client != null)
            {
                MESSAGE message = MESSAGE.From(e);
                this.OnMessage(client, message);
            }
        }

        public virtual void Start()
        {
            this.m_poListener.Start();
        }

        public virtual void Stop()
        {
            this.m_poListener.Stop();
        }

        public virtual ISocket GetChannel(ApplicationType applicationType, Guid guid)
        {
            var clientTable = GetSocketTable(applicationType);
            lock (clientTable)
            {
                clientTable.TryGetValue(guid, out SocketMvhClient client);
                return client;
            }
        }

        public virtual ISocket CloseChannel(ApplicationType applicationType, Guid guid)
        {
            SocketMvhClient client = null;
            var clientTable = GetSocketTable(applicationType);
            lock (clientTable)
            {
                if (!clientTable.TryRemove(guid, out client))
                {
                    client = null;
                }
            }
            if (client != null)
            {
                client.Close();
            }
            return client;
        }

        public virtual int AvailableChannels => this.m_poSocketTable.Count;

        public virtual IEnumerator<SocketMvhClientTable> GetEnumerator()
        {
            return m_poSocketTable.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void OnMessage(SocketMvhClient socket, MESSAGE message)
        {
            this.MessageQueue.AddMessage(socket, message);
        }

        protected virtual void OnOpen(SocketMvhClient socket)
        {
            if (false == this.GetHandler()?.ProcessAccept(socket))
            {
                socket.Close();
                return;
            }
            this.Open?.Invoke(socket, socket);
        }

        protected virtual void OnClose(SocketMvhClient socket)
        {
            this.GetHandler()?.ProcessClose(socket);
            this.Close?.Invoke(socket, socket);
        }

        protected virtual SocketMvhContext CreateContext(SocketMvhClient socket, MESSAGE message)
        {
            return new SocketMvhContext(this, socket, message);
        }

        public virtual void ProcessMessage(ISocket socket, MESSAGE message)
        {
            if (!this.Retransmission.AckRetransmission(applicationType: socket.ApplicationType, id: socket.Id,
                   commands: message.CommandId, ackNo: message.SequenceNo, message: message))
            {
                if (socket is SocketMvhClient sender)
                {
                    sender.OnMessage(message);
                    if (true != this.GetHandler()?.ProcessMessage(sender, message))
                    {
                        ISocketMvhHandler handler = this.HandlerContainer.GetHandler(socket.ApplicationType, message.CommandId);
                        if (handler != null)
                        {
                            SocketMvhContext context = CreateContext(sender, message);
                            handler.ProcessRequest(context);
                        }
                    }

                    this.Message?.Invoke(socket, message);
                }
            }
        }

        IEnumerator<ApplicationType> IEnumerable<ApplicationType>.GetEnumerator()
        {
            return GetAllApplicationType().GetEnumerator();
        }

        public virtual IEnumerable<string> GetAllPlatformNames(ApplicationType applicationType) => m_poApplicationChannelMapping.GetMappingObject(applicationType)?.GetAllPlatformNames();

        public virtual IEnumerable<KeyValuePair<int, ISocket>> GetAllSockets(ApplicationType applicationType, string platform) => m_poApplicationChannelMapping.GetMappingObject(applicationType)?.GetAllSockets(platform);

        public virtual ISocket GetChannel(ApplicationType applicationType, string platform, int sid) => m_poApplicationChannelMapping.GetMappingObject(applicationType)?.GetChannel(platform, sid);
    }
}
