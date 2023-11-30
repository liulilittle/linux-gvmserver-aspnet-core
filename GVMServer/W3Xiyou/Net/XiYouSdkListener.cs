namespace GVMServer.W3Xiyou.Net
{
    using GVMServer.Net;
    using System.Net.Sockets;

    public class XiYouSdkListener : SocketListener
    {
        public XiYouSdkListener(int port) : base(port)
        {

        }

        protected override ISocketClient CreateClient(Socket socket)
        {
            return new XiYouSdkClient(this, socket);
        }
    }
}
