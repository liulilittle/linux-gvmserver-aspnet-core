namespace GVMServer.Ns.Net.Model
{
    using System;
    using GVMServer.Ns.Functional;

    public class MatchAvavailableServerRequest : EventArgs
    {
        public ApplicationType ApplicationType { get; set; }

        public string Platform { get; set; }
    }

    public class MatchAvavailableServerResponse : EventArgs
    {
        public Ns Credentials { get; set; }
    }
}
