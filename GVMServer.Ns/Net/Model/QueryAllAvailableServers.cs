namespace GVMServer.Ns.Net.Model
{
    using System;
    using System.Collections.Generic;
    using GVMServer.Ns.Functional;

    public class QueryAllAvailableServersRequest : EventArgs
    {
        public ApplicationType ApplicationType { get; set; }

        public string Platform { get; set; }
    }

    public class QueryAllAvailableServersResponse : EventArgs
    {
        public ApplicationType ApplicationType { get; set; }

        public string Platform
        {
            get; set;
        }

        public IList<Ns> Credentials { get; set; }
    }
}
