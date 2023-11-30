namespace GVMServer.W3Xiyou.Net.Mvh
{
    public abstract class SocketHandler
    {
        public virtual void ProcessRequest(SocketContext context)
        {

        }

        public virtual void ProcessResponse(SocketContext context)
        {

        }

        public virtual void ProcessTimeout( SocketContext context )
        {

        }
    }
}
