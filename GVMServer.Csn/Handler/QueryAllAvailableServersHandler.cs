namespace GVMServer.Csn.Handler
{
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net.Model;
    using GVMServer.Ns.Net.Mvh;
    using GVMServer.Ns.Functional;
    using System.Collections.Generic;

    [SocketMvhHandler(ApplicationType = new[] { ApplicationType.ApplicationType_GameServer, ApplicationType.ApplicationType_CrossServer },
        CommandId = Commands.Commands_QueryAllAvailableServers)]
    public class QueryAllAvailableServersHandler : ISocketMvhHandler
    {
        public virtual int AckTimeout => 1000;

        public virtual int AckRetransmission => 3;

        public void ProcessRequest(SocketMvhContext context)
        {
            QueryAllAvailableServersResponse response = new QueryAllAvailableServersResponse();
            response.Credentials = new List<Ns>();
            do
            {
                QueryAllAvailableServersRequest request = context.Request.Read<QueryAllAvailableServersRequest>();
                if (request == null)
                {
                    break;
                }
                else
                {
                    response.ApplicationType = request.ApplicationType;
                    response.Platform = request.Platform;
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

                    response.Credentials.Add(credentials);
                }
            } while (false);
            context.Response.Write(response, AckTimeout, AckRetransmission);
        }
    }
}
