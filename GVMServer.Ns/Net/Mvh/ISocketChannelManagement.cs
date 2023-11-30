namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Collections.Generic;

    public interface ISocketChannelManagement : IEnumerable<ApplicationType>
    {
        SocketMvhHandlerContainer HandlerContainer { get; }

        SocketMvhRetransmission Retransmission { get; }

        ISocket CloseChannel(ApplicationType applicationType, Guid guid);

        ISocket GetChannel(ApplicationType applicationType, Guid guid);

        void ProcessMessage(ISocket socket, Message message);

        IEnumerable<ISocket> GetAllSockets(ApplicationType applicationType);

        IEnumerable<ApplicationType> GetAllApplicationType();

        IEnumerable<string> GetAllPlatformNames(ApplicationType applicationType);

        ISocket GetChannel(ApplicationType applicationType, string platform, int sid);

        IEnumerable <KeyValuePair<int, ISocket>> GetAllSockets(ApplicationType applicationType, string platform);
    }
}
