namespace GVMServer.Ns.Net.Model
{
    using System;

    public class AbortSocketMessage : EventArgs
    {
        public Guid Id { get; set; }

        public ApplicationType ApplicationType { get; set; }
    }
}
