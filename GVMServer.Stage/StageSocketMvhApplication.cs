namespace GVMServer.Stage
{
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Mvh;

    public class StageSocketMvhApplication : SocketMvhApplication
    {
        private readonly StageServerHandler m_poStageServerHandler;

        public StageSocketMvhApplication(ISocketHandler handler, int port, int maxRetransmissionConcurrent) : base(handler, port, maxRetransmissionConcurrent)
        {
            this.m_poStageServerHandler = new StageServerHandler(this, handler);
        }

        public override IServerHandler GetHandler()
        {
            return this.m_poStageServerHandler;
        }
    }
}
