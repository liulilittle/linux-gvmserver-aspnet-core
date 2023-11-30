namespace GVMServer.Ns.Net.Handler
{
    using System;
    using GVMServer.Net;
    using GVMServer.Ns.Net.Model;
    using Ns = GVMServer.Ns.Functional.Ns;

    public interface ISocketHandler
    {
        bool ProcessLinkHeartbeat(NsSocket socket, long ackNo, LinkHeartbeat heartbeat);

        bool ProcessAuthentication(NsSocket socket, long ackNo, AuthenticationRequest authentication, Action<Ns> credentials);

        bool InitiateReportLinkHeartbeat(NsSocket socket);

        bool InitiateAuthenticationAsync(NsSocket socket);

        bool ProcessAbort(NsSocket socket);

        bool ProcessAccept(NsSocket socket);

        bool ProcessMessage(NsSocket socket, SocketMessage message);
    }
}
