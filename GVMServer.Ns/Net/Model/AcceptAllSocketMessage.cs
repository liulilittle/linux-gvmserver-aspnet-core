namespace GVMServer.Ns.Net.Model
{
    using System;
    using System.Collections.Generic;

    public class AcceptAllSocketMessage : EventArgs
    {
        public IList<AcceptSocketMessage> AcceptSockets { get; set; }
    }
}
