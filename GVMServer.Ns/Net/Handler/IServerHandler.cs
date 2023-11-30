namespace GVMServer.Ns.Net.Handler
{
    using GVMServer.Ns.Net.Mvh;

    public interface IServerHandler
    {
        bool ProcessMessage(SocketMvhClient socket, Message message);

        bool ProcessAccept(SocketMvhClient socket);

        bool ProcessClose(SocketMvhClient socket);
    }
}
