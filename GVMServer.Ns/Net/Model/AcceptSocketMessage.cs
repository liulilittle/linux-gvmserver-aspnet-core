namespace GVMServer.Ns.Net.Model
{
    using Ns = GVMServer.Ns.Functional.Ns;

    public class AcceptSocketMessage : AbortSocketMessage
    {
        public Ns Credentials { get; set; }
    }
}
