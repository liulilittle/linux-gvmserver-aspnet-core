namespace GVMServer.Net
{
    using System;
    using System.Net;

    public interface ISocketClient
    {
        event EventHandler<SocketMessage> OnMessage;
        event EventHandler OnOpen;
        event EventHandler OnClose;
        event EventHandler OnError;

        bool Available { get; }

        bool Blocking { get; }

        bool Open(EndPoint remoteEP);

        void Abort();

        void Close();

        bool Send(SocketMessage message);

        EndPoint RemoteEndPoint { get; }

        EndPoint LocalEndPoint { get; }

        SocketListener Listener { get; }
    }
}
