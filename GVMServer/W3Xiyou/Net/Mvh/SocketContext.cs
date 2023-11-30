namespace GVMServer.W3Xiyou.Net.Mvh
{
    using GVMServer.Net;

    public class SocketContext
    {
        private readonly SocketMessage message;
        private readonly SocketHandler handler;
        private readonly ISocketClient socket;
        private readonly SocketHandlerAttribute handlerAttribute;

        protected internal SocketContext(SocketMvhApplication application, SocketHandler handler,
            SocketHandlerAttribute handlerAttribute, SocketMessage message)
        {
            this.Application = application;
            this.message = message;
            this.handler = handler;
            this.socket = message.GetClient();
            this.handlerAttribute = handlerAttribute;
            this.Request = new SocketRequest(this, message);
            this.Response = new SocketResponse(this, message);
        }

        public SocketMvhApplication Application { get; }

        public SocketRequest Request { get; }

        public SocketResponse Response { get; }

        public SocketHandler GetHandler() => this.handler;

        public ISocketClient GetClient() => this.socket;

        public SocketHandlerAttribute GetHandlerAttribute() => this.handlerAttribute;

        public object Tag { get; set; }
    }
}
