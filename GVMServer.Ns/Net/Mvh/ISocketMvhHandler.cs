namespace GVMServer.Ns.Net.Mvh
{
    public interface ISocketMvhHandler
    {
        void ProcessRequest(SocketMvhContext context);
    }
}
