namespace GVMServer.Net
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    public class SocketListener
    {
        private readonly AsyncCallback m_acceptSocket;
        private volatile Socket m_server;
        private readonly object m_syncobj = new object();

        private readonly EventHandler<SocketMessage> m_onMessage;
        private readonly EventHandler m_onClose;
        private readonly EventHandler m_onOpen;

        public event EventHandler<SocketMessage> Message;
        public event EventHandler<EventArgs> Open;
        public event EventHandler<EventArgs> Close;

        public SocketListener(int port)
        {
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException("Port less than 0 or greater than 65535");
            }
            this.m_acceptSocket = new AsyncCallback(this.AcceptSocket);
            this.Port = port;
            this.m_onClose = (sender, e) =>
            {
                if (sender is ISocketClient socket) // 避免托管泄漏
                {
                    lock (socket)
                    {
                        socket.OnClose -= this.m_onClose;
                        socket.OnError -= this.m_onClose;
                        socket.OnOpen -= this.m_onOpen;
                        socket.OnMessage -= this.m_onMessage;
                    }
                    this.OnClose(socket);
                }
            };
            this.m_onOpen = (sender, e) =>
            {
                if (sender is ISocketClient socket)
                {
                    this.OnOpen(socket);
                }
            };
            this.m_onMessage = (sender, e) => this.OnMessage(e);
        }

        protected virtual void OnClose(ISocketClient e)
        {
            CallEvent(this.Close, e, EventArgs.Empty);
        }

        protected virtual void OnOpen(ISocketClient e)
        {
            CallEvent(this.Open, e, EventArgs.Empty);
        }

        public int Port { get; }

        public void Stop()
        {
            lock (this.m_syncobj)
            {
                if (this.m_server != null)
                {
                    CloseSocket(this.m_server);
                    this.m_server = null;
                }
            }
        }

        private static void CloseSocket(Socket socket)
        {
            if (socket != null)
            {
                if (socket.Connected)
                {
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception) { }
                }
                socket.Close();
                socket.Dispose();
            }
        }

        public bool Runing
        {
            get
            {
                return this.m_server != null;
            }
        }

        public void Start()
        {
            lock (this.m_syncobj)
            {
                if (this.m_server == null)
                {
                    this.m_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    this.m_server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    this.m_server.Bind(new IPEndPoint(IPAddress.Any, this.Port));
                    this.m_server.Listen(ushort.MaxValue);
                    this.AcceptSocket(null);
                }
            }
        }

        private void AcceptSocket(IAsyncResult ar)
        {
            bool callAcceptSocket = false;
            try
            {
                Socket server = null;
                lock (this.m_syncobj)
                {
                    server = this.m_server;
                }
                if (server == null)
                {
                    return;
                }
                else if (ar == null)
                {
                    server.BeginAccept(this.m_acceptSocket, null);
                }
                else
                {
                    Socket socket = server.EndAccept(ar);
                    if (socket != null)
                    {
                        ISocketClient client = CreateClient(socket);
                        lock (client)
                        {
                            client.OnOpen += this.m_onOpen;
                            client.OnMessage += this.m_onMessage;
                            client.OnClose += this.m_onClose;
                            client.OnError += this.m_onClose;
                        }
                        this.OpenClient(client);
                    }
                    callAcceptSocket = true;
                }
            }
            catch (Exception) // 链接可能在未完成“EndAccept”的时候出现故障。
            {
                callAcceptSocket = true;
            }
            if (callAcceptSocket)
            {
                this.AcceptSocket(null);
            }
        }

        protected virtual void OpenClient(ISocketClient client)
        {
            if (client != null)
            {
                client.Open(default(EndPoint));
            }
        }

        protected virtual void OnMessage(SocketMessage e)
        {
            var events = this.Message;
            events?.Invoke(this, e);
        }

        private static void CallEvent<TEventArgs>(EventHandler<TEventArgs> events, object sender, TEventArgs e)
        {
            events?.Invoke(sender, e);
        }

        protected virtual ISocketClient CreateClient(Socket socket)
        {
            return new SocketClient(this, socket);
        }
    }
}
