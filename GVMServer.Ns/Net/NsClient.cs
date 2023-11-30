namespace GVMServer.Ns.Net
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using GVMServer.Net;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Threading;

    public class NsClient : EventArgs, IDisposable, ISocket
    {
        private EventHandler onCloseHandler;
        private EventHandler onOpenHandler;
        private EventHandler<SocketMessage> onMessageHandler;

        private volatile bool opened;
        private volatile bool disposed;
        private volatile bool started;
        private volatile int availableSockets;
        private readonly LinkedList<NsSocket> sockets;
        private readonly object syncobj = new object();
        private IDictionary<NsSocket, LinkedListNode<NsSocket>> socketsNode;

        public event EventHandler<EventArgs> OnOpen;
        public event EventHandler<EventArgs> OnAbort;
        public event EventHandler<Message> OnMessage;

        public int MaxChannelNumber { get; }

        public int AvailableChannels => availableSockets;

        public bool IsDisposed
        {
            get
            {
                lock (this.syncobj)
                {
                    return this.disposed;
                }
            }
        }

        public Guid AuthenticationId { get; }

        public ApplicationType AuthenticationType { get; }

        public Guid Id { get; }

        public ApplicationType ApplicationType { get; }

        public EndPoint RemoteEndPoint { get; }

        public ISocketHandler SocketHandler { get; }

        public bool Available => this.AvailableChannels > 0;

        public Ns Credentials { get; private set; }

        public NsClient(ISocketHandler handler, ApplicationType applicationType, Guid id, ApplicationType authenticationType, Guid authenticationId, string address, int maxChannelNumber)
        {
            this.SocketHandler = handler ?? throw new ArgumentNullException(nameof(handler));

            IPEndPoint server = GetAddress(address);
            if (maxChannelNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxChannelNumber));
            }

            this.RemoteEndPoint = server;
            this.AuthenticationType = authenticationType;
            this.AuthenticationId = authenticationId;
            this.Id = id;
            this.ApplicationType = applicationType;

            this.availableSockets = 0;
            this.MaxChannelNumber = maxChannelNumber;
            this.onCloseHandler = (sender, e) =>
            {
                NsSocket socket = (NsSocket)sender;
                lock (this.syncobj)
                {
                    this.socketsNode.Remove(socket, out LinkedListNode<NsSocket> node);
                    if (node != null)
                    {
                        if (this.availableSockets > 0)
                        {
                            --this.availableSockets;
                        }
                        this.sockets.Remove(node);
                    }
                    if (this.started)
                    {
                        Timer reconnaissanceTimer = new Timer(300);
                        reconnaissanceTimer.Tick += (aa, bb) =>
                        {
                            reconnaissanceTimer.Close();
                            reconnaissanceTimer.Dispose();
                            DynamicOpenNewSocket(this.RemoteEndPoint);
                        };
                        reconnaissanceTimer.Start();
                    }
                }
                this.DoClose(socket);
            };
            this.onOpenHandler = (sender, e) =>
            {
                NsSocket socket = (NsSocket)sender;
                lock (this.syncobj)
                {
                    this.availableSockets++;

                    var credentials = socket.Credentials;
                    if (credentials != null)
                    {
                        this.Credentials = credentials;
                    }
                }
                this.DoOpen(socket);
            };
            this.onMessageHandler = (sender, e) =>
            {
                Commands commands = (Commands)e.CommandId;
                if (commands == Commands.Commands_Authentication)
                {
                    return;
                }

                NsSocket socket = (NsSocket)sender;
                Message message = Message.From(e);
                if (message == null)
                {
                    socket.Abort();
                }
                else
                {
                    message.CommandId = commands;
                    message.SequenceNo = e.SequenceNo;
                    this.DoMessage(socket, message);
                }
            };

            this.sockets = new LinkedList<NsSocket>();
            this.socketsNode = new ConcurrentDictionary<NsSocket, LinkedListNode<NsSocket>>();
        }

        ~NsClient()
        {
            this.Dispose();
        }

        protected virtual void DoOpen(NsSocket socket)
        {
            bool callEvent = false;
            lock (this.syncobj)
            {
                if (this.availableSockets > 0 && !this.opened)
                {
                    callEvent = true;
                    this.opened = true;
                }
            }
            if (callEvent)
            {
                var events = this.OnOpen;
                events?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void DoClose(NsSocket socket)
        {
            bool callEvent = false;
            lock (this.syncobj)
            {
                if (this.availableSockets <= 0 && this.opened)
                {
                    callEvent = true;
                    this.opened = false;
                }
            }
            if (callEvent)
            {
                var events = this.OnAbort;
                events?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void DoMessage(NsSocket socket, Message message)
        {
            var events = this.OnMessage;
            if (events != null)
            {
                events(this, message);
            }
        }

        private void CheckAndThrowObjectDisposedException()
        {
            bool ep = false;
            lock (this.syncobj)
            {
                ep = this.disposed;
            }

            if (ep)
            {
                throw new ObjectDisposedException("Managed and unmanaged resources currently held by the NsClient object instance have been requested for release");
            }
        }

        public virtual NsClient Run()
        {
            CheckAndThrowObjectDisposedException();

            Exception exception = null;
            lock (this.syncobj)
            {
                do
                {
                    if (this.started || this.sockets.Count > 0)
                    {
                        exception = new
                            InvalidOperationException("The current operation is invalid but an ununderstood error was found; no NsSocket instances are available");
                        break;
                    }

                    this.started = true;

                    for (int i = 0; i < this.MaxChannelNumber; i++)
                    {
                        DynamicOpenNewSocket(this.RemoteEndPoint);
                    }
                } while (false);
            }
            if (exception != null)
            {
                throw exception;
            }
            return this;
        }

        private NsSocket DynamicOpenNewSocket(EndPoint remoteEP)
        {
            var session = new NsSocket(this.SocketHandler, this.AuthenticationType, this.AuthenticationId);
            do
            {
                ISocketClient socket = session;
                socket.OnOpen += onOpenHandler;
                socket.OnClose += onCloseHandler;
                socket.OnError += onCloseHandler;
                socket.OnMessage += onMessageHandler;
            } while (false);
            this.socketsNode.Add(session, this.sockets.AddLast(session));
            session.Open(remoteEP);
            return session;
        }
   
        public static IPEndPoint GetAddress(string address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            address = address.TrimStart().TrimEnd();
            if (address.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(address));
            }

            int port = address.LastIndexOf(':');
            if (port < 0)
            {
                port = 0;
            }
            else
            {
                string s = address.Substring(1 + port);
                address = address.Substring(0, port);

                if (!int.TryParse(s, out port))
                {
                    throw new ArgumentOutOfRangeException("Unable to convert the port number");
                }

                if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                {
                    throw new ArgumentOutOfRangeException($"Port less than {IPEndPoint.MinPort} or greater than {IPEndPoint.MaxPort}");
                }
            }

            if (IPAddress.TryParse(address, out IPAddress ip) && ip != null)
            {
                return new IPEndPoint(ip, port);
            }

            IPAddress[] addresses = Dns.GetHostAddresses(address);
            if (addresses == null || addresses.Length <= 0)
            {
                throw new ArgumentOutOfRangeException("The number address associated with the current host cannot be retrieved");
            }

            IPAddress ipa = addresses.FirstOrDefault(i =>
            {
                if (i == null)
                {
                    return false;
                }

                if (i.AddressFamily != AddressFamily.InterNetwork)
                {
                    return false;
                }

                if (i == IPAddress.Any || i == IPAddress.None || i == IPAddress.Broadcast)
                {
                    return false;
                }

                return true;
            });
            if (ipa != null)
            {
                return new IPEndPoint(ipa, port);
            }

            ipa = addresses.FirstOrDefault(i =>
            {
                if (i == null)
                {
                    return false;
                }

                if (i.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    return false;
                }

                if (i == IPAddress.IPv6Any || i == IPAddress.IPv6None)
                {
                    return false;
                }

                return true;
            });
            if (ipa != null)
            {
                return new IPEndPoint(ipa, port);
            }

            throw new ArgumentOutOfRangeException("A valid AddressFamily InterNetwork or InterNetworkV6 endpoint address was not found");
        }

        public virtual NsSocket GetCurrentChannel()
        {
            CheckAndThrowObjectDisposedException();
            lock (this.syncobj)
            {
                var node = this.sockets.First;
                if (node == null)
                {
                    return null;
                }

                while (node != null)
                {
                    var current = node;
                    node = node.Next;

                    var socket = current.Value;
                    if (socket.Available)
                    {
                        return socket;
                    }

                    if (this.sockets.Count > 1)
                    {
                        this.sockets.Remove(current);
                        this.sockets.AddLast(current);
                    }
                }

                return null;
            }
        }

        public virtual bool Send(Message message)
        {
            if (message == null)
            {
                return false;
            }

            for (var i = 0; i < this.AvailableChannels; i++)
            {
                NsSocket channel = this.GetCurrentChannel();
                if (channel == null)
                {
                    return false;
                }

                if (!channel.Available)
                {
                    continue;
                }

                SocketMessage packet = new SocketMessage(channel,
                    (ushort)message.CommandId,
                    message.SequenceNo,
                    0,
                    message.Payload.Buffer,
                    message.Payload.Offset,
                    message.Payload.Length);
                if (!channel.Send(packet))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public virtual void Close()
        {
            lock (this.syncobj)
            {
                if (this.disposed || !this.started)
                {
                    return;
                }

                this.started = false;

                var node = this.sockets.First;
                while (node != null)
                {
                    var current = node;
                    node = node.Next;

                    ISocketClient socket = current.Value;
                    if (socket != null)
                    {
                        socket.Close();
                        socket.OnOpen -= onOpenHandler;
                        socket.OnClose -= onCloseHandler;
                        socket.OnError -= onCloseHandler;
                        socket.OnMessage -= onMessageHandler;

                        current.Value = null;
                    }
                }
                this.availableSockets = 0;
                this.sockets.Clear();
                this.socketsNode.Clear();
            }
        }

        public virtual void Dispose()
        {
            lock (this.syncobj)
            {
                if (!this.disposed)
                {
                    this.Close();
                    this.onMessageHandler = null;
                    this.onCloseHandler = null;
                    this.onOpenHandler = null;
                    this.OnOpen = null;
                    this.OnAbort = null;
                    this.OnMessage = null;
                    this.disposed = true;
                }
            }
            GC.SuppressFinalize(this);
        }

        public static bool Echo(string address, Action<bool> success)
        {
            if (success == null)
            {
                return false;
            }

            IPEndPoint server = null;
            try
            {
                server = GetAddress(address);
                if (server == null)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            void closesocket(Socket s)
            {
                try
                {
                    s.Shutdown(SocketShutdown.Send);
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {

                }
                try
                {
                    s.Close();
                    s.Dispose();
                }
                catch (Exception)
                {

                }
            }

            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            AsyncCallback callback = null;
            bool ok = false;
            try
            {
                callback = (ar) =>
                {
                    bool echo = false;
                    try
                    {
                        socket.EndConnect(ar);
                        echo = true;
                    }
                    catch (Exception)
                    {

                    }
                    closesocket(socket);
                    success(echo);
                };
                ok = null != socket.BeginConnect(server, callback, null);
            }
            catch (Exception)
            {

            }
            callback = null;
            if (!ok)
            {
                closesocket(socket);
                success(ok);
            }
            return ok;
        }

        public static bool Echo(IEnumerable<string> nodehosts, int port, Action<string, int> success)
        {
            if (port <= IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                return false;
            }

            if (nodehosts == null)
            {
                return false;
            }

            var enumerator = nodehosts.GetEnumerator();
            if (enumerator == null)
            {
                return false;
            }

            bool EchoNextNodehost(out string s)
            {
                s = null;
                while (true)
                {
                    if (!enumerator.MoveNext())
                    {
                        return false;
                    }

                    string current = enumerator.Current;
                    if (string.IsNullOrEmpty(current))
                    {
                        continue;
                    }

                    s = current;
                    return true;
                }
            };
            bool EchoEchoNodehost()
            {
                if (!EchoNextNodehost(out string nodehost))
                {
                    return false;
                }

                if (Echo($"{nodehost}:{port}", (successes) =>
                {
                    if (successes)
                    {
                        success?.Invoke(nodehost, port);
                    }
                    else
                    {
                        EchoEchoNodehost();
                    }
                }))
                {
                    return true;
                }
                else
                {
                    return EchoEchoNodehost();
                }
            };
            return EchoEchoNodehost();
        }
    }
}
