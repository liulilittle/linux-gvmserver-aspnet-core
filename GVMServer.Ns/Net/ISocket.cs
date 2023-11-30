namespace GVMServer.Ns.Net
{
    using System;
    using Ns = GVMServer.Ns.Functional.Ns;

    public interface ISocket
    {
        bool Available { get; }

        Guid Id { get; }

        ApplicationType ApplicationType { get; }

        Ns Credentials { get; }

        void Close();

        bool Send(Message message);
    }
}
