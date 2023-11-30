namespace GVMServer.Ns.Net
{
    using System;
    using System.Net.Sockets;
    using GVMServer.Net;
    using GVMServer.Ns.Net.Handler;

    public class NsListener : SocketListener
    {
        public ISocketHandler SocketHandler { get; }

        public NsListener(ISocketHandler handler, int port) : base(port)
        {
            this.SocketHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        protected override ISocketClient CreateClient(Socket socket)
        {
            return new NsSocket(this, socket);
        }

        protected override void OnClose(ISocketClient e)
        {
            base.OnClose(e);
        }

        protected override void OnMessage(SocketMessage e)
        {
            base.OnMessage(e);
        }

        protected override void OnOpen(ISocketClient e)
        {
            base.OnOpen(e);
        }

        protected override void OpenClient(ISocketClient client)
        {
            base.OpenClient(client);
        }
    }
}
