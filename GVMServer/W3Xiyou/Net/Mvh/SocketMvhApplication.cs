namespace GVMServer.W3Xiyou.Net.Mvh
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using GVMServer.Net;
    using Timer = GVMServer.Threading.Timer;

    public class SocketMvhApplication
    {
        private class PlatformContext
        {
            public readonly ConcurrentDictionary<long, ISocketClient> identifier2Sockets =
                 new ConcurrentDictionary<long, ISocketClient>();
            public readonly ConcurrentDictionary<string, ISocketClient> nomenclature2Sockets =
                new ConcurrentDictionary<string, ISocketClient>();
        }

        private readonly XiYouSdkListener listener = null;
        private readonly object syncobj = new object();
        private readonly Timer doeventstimer = null;
        private readonly Timer doclienteventtimer = null;
        private readonly ConcurrentDictionary<string, PlatformContext> platforms = new ConcurrentDictionary<string, PlatformContext>();
        private readonly static ISocketClient[] emptysocketclients = new ISocketClient[0];
        private readonly LinkedList<ISocketClient> socketslinkedlist = new LinkedList<ISocketClient>();
        private volatile LinkedListNode<ISocketClient> socketslinkedcurrent = null;
        private readonly ConcurrentDictionary<ISocketClient, LinkedListNode<ISocketClient>> socketslinkednodes =
            new ConcurrentDictionary<ISocketClient, LinkedListNode<ISocketClient>>();

        public SocketHandlerContainer Handlers { get; } = new SocketHandlerContainer();

        public SocketRequestInvoker Invoker { get; }

        private PlatformContext GetPlatformContext(string platform, bool initialize = false)
        {
            lock (this.syncobj)
            {
                platform = platform ?? string.Empty;
                if (!platforms.TryGetValue(platform, out PlatformContext context))
                {
                    if (!initialize)
                    {
                        return null;
                    }
                    context = new PlatformContext();
                    platforms.TryAdd(platform, context);
                }
                return context;
            }
        }

        public virtual int MaxTickConcurrent { get; } = 10;

        private class SocketMvhListener : XiYouSdkListener
        {
            public SocketMvhApplication Mvh { get; }

            public SocketMvhListener(SocketMvhApplication mvh, int port) : base(port)
            {
                this.Mvh = mvh ?? throw new ArgumentNullException(nameof(mvh));
            }

            protected override void OnOpen(ISocketClient e)
            {
                if (!(e is XiYouSdkClient socket))
                {
                    e.Abort();
                }
                else if (!string.IsNullOrEmpty(socket.Nomenclature))
                {
                    PlatformContext context = this.Mvh.GetPlatformContext(socket.Platform, true);
                    lock (this.Mvh.syncobj)
                    {
                        context.nomenclature2Sockets.TryRemove(socket.Nomenclature, out ISocketClient ov2);
                        if (context.identifier2Sockets.TryRemove(socket.Identifier, out ISocketClient ov1))
                        {
                            if (ov1 != null)
                            {
                                this.Mvh.socketslinkednodes.TryRemove(ov1, out LinkedListNode<ISocketClient> socketlinkednodec);
                                if (socketlinkednodec != null)
                                {
                                    this.Mvh.socketslinkedlist.Remove(socketlinkednodec);
                                }
                                ov1.Abort();
                            }
                        }
                        LinkedListNode<ISocketClient> socketlinkednode = this.Mvh.socketslinkedlist.AddLast(e);
                        if (socketlinkednode == null)
                        {
                            e.Abort();
                        }
                        else
                        {
                            this.Mvh.socketslinkednodes.TryAdd(e, socketlinkednode);
                        }
                        context.identifier2Sockets.TryAdd(socket.Identifier, e);
                        context.nomenclature2Sockets.TryAdd(socket.Nomenclature, e);
                    }
                    this.Mvh.OnOpen(socket);
                }
                base.OnOpen(e);
            }

            protected override void OnMessage(SocketMessage e)
            {
                base.OnMessage(e);
                this.Mvh.OnMessage(e);
            }

            protected override void OnClose(ISocketClient e)
            {
                if (e is XiYouSdkClient socket && !string.IsNullOrEmpty(socket.Nomenclature))
                {
                    PlatformContext context = this.Mvh.GetPlatformContext(socket.Platform);
                    if (context != null)
                    {
                        lock (this.Mvh.syncobj)
                        {
                            context.identifier2Sockets.TryRemove(socket.Identifier, out ISocketClient ov1);
                            context.nomenclature2Sockets.TryRemove(socket.Nomenclature, out ISocketClient ov2);
                            if (context.identifier2Sockets.IsEmpty || context.nomenclature2Sockets.IsEmpty)
                            {
                                context.identifier2Sockets.Clear();
                                context.nomenclature2Sockets.Clear();
                                this.Mvh.platforms.TryRemove(socket.Platform, out PlatformContext contextc);
                            }
                            this.Mvh.socketslinkednodes.TryRemove(e, out LinkedListNode<ISocketClient> socketlinkednode);
                            if (socketlinkednode != null)
                            {
                                this.Mvh.socketslinkedlist.Remove(socketlinkednode);
                            }
                        }
                    }
                    this.Mvh.OnClose(e);
                }
                base.OnClose(e);
            }
        }

        public SocketMvhApplication(int port)
        {
            this.listener = new SocketMvhListener(this, port);
            this.Invoker = this.CreateInvoker();
            this.doeventstimer = new Timer(100);
            this.doeventstimer.Tick += (sender, e) => this.DoEvents();
            this.doeventstimer.Start();
            this.doclienteventtimer = new Timer(1);
            this.doclienteventtimer.Tick += (sender, e) =>
            {
                ISocketClient socket = null;
                lock (this.syncobj)
                {
                    int concurrent = Math.Min(socketslinkedlist.Count, this.MaxTickConcurrent);
                    for (int i = 0; i < concurrent; i++)
                    {
                        if (socketslinkedcurrent == null)
                        {
                            socketslinkedcurrent = socketslinkedlist.First;
                        }
                        else
                        {
                            socketslinkedcurrent = socketslinkedcurrent.Next;
                        }
                        if (socketslinkedcurrent == null)
                        {
                            break;
                        }
                        else
                        {
                            socket = socketslinkedcurrent.Value;
                        }
                        if (socket is XiYouSdkClient client)
                        {
                            client.DoEvents();
                        }
                    }
                }
            };
            this.doclienteventtimer.Start();
        }

        protected virtual SocketRequestInvoker CreateInvoker()
        {
            return new SocketRequestInvoker(this);
        }

        protected virtual void DoEvents()
        {
            this.Invoker.DoEvents(null);
        }

        protected virtual void OnMessage(SocketMessage e)
        {
            SocketHandlerContainer.HandlerObject handlerObject = this.Handlers.GetHandler(e.CommandId);
            if (handlerObject == null) // 有人在跑服务器
            {
                e.GetClient().Close();
            }
            else
            {
                SocketHandler handler = handlerObject.Handler;
                SocketContext context = new SocketContext(this, handler, handlerObject.Attribute, e);
                this.Invoker.DoEvents(e);
                handler.ProcessRequest(context);
            }
        }

        protected virtual void OnClose(ISocketClient e)
        {

        }

        protected virtual void OnOpen(ISocketClient e)
        {

        }

        public virtual SocketHandlerAttribute GetAttribute(ushort commandId) => this.Handlers.GetHandler(commandId)?.Attribute;

        public virtual SocketHandler GetHandler(ushort commandId) => this.Handlers.GetHandler(commandId)?.Handler;

        public virtual ISocketClient GetClient(string platform, long identifier)
        {
            if (0 == identifier)
            {
                return null;
            }
            PlatformContext context = GetPlatformContext(platform);
            if (context == null)
            {
                return null;
            }
            context.identifier2Sockets.TryGetValue(identifier, out ISocketClient socket);
            return socket;
        }

        public virtual long GetIdentifier(ISocketClient socket)
        {
            if (!(socket is XiYouSdkClient xy))
            {
                throw new ArgumentOutOfRangeException(nameof(socket));
            }
            return xy.Identifier;
        }

        public virtual IEnumerable<ISocketClient> GetAllClient(string platform)
        {
            PlatformContext context = GetPlatformContext(platform);
            if (context == null)
            {
                return emptysocketclients;
            }
            return context.identifier2Sockets.Values;
        }

        public virtual ISocketClient GetClient(string platform, string nomenclature)
        {
            if (string.IsNullOrEmpty(nomenclature))
            {
                return null;
            }
            PlatformContext context = GetPlatformContext(platform);
            if (context == null)
            {
                return null;
            }
            context.nomenclature2Sockets.TryGetValue(nomenclature, out ISocketClient socket);
            return socket;
        }

        public virtual void Start()
        {
            this.listener.Start();
        }

        public virtual void Stop()
        {
            this.listener.Stop();
        }
    }
}
