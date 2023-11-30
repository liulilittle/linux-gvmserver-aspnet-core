namespace GVMServer.Csn.Handler
{
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net.Model;
    using GVMServer.Ns.Net.Mvh;

    [SocketMvhHandler(ApplicationType = new[] { ApplicationType.ApplicationType_GameServer, ApplicationType.ApplicationType_CrossServer }, 
        CommandId = Commands.Commands_MatchAvavailableServer)]
    public class MatchAvavailableServerHandler : ISocketMvhHandler
    {
        public virtual int AckTimeout => 1000;

        public virtual int AckRetransmission => 3;

        public void ProcessRequest(SocketMvhContext context)
        {
            MatchAvavailableServerResponse response = new MatchAvavailableServerResponse
            {
                Credentials = null
            };
            do
            {
                MatchAvavailableServerRequest request = context.Request.Read<MatchAvavailableServerRequest>();
                if (request == null)
                {
                    break;
                }

                ISocketChannelManagement managements = context.GetChannelManagement();
                if (managements == null)
                {
                    break;
                }

                var sockets = managements.GetAllSockets(request.ApplicationType, request.Platform);
                if (sockets == null)
                {
                    break;
                }

                foreach (var pair in sockets)
                {
                    var socket = pair.Value;
                    if (socket == null)
                    {
                        continue;
                    }

                    var credentials = socket.Credentials;
                    if (credentials == null)
                    {
                        continue;
                    }

                    response.Credentials = credentials;
                    break;
                }
            } while (false);
            context.Response.Write(response, AckTimeout, AckRetransmission);
        }
    }
}
