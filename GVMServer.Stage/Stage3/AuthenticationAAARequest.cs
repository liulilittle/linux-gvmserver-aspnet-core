namespace GVMServer.Stage.Stage3
{
    public class AuthenticationAAARequest
    {
        public ushort SvrAreaNo { get; set; }

        public ushort ServerNo { get; set; }

        public string Platform { get; set; }

        public byte LinkType { get; set; }

        public ushort MaxFD { get; set; }
    }
}
