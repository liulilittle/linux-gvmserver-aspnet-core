namespace GVMServer.Stage.Stage3
{
    public class AuthenticationAAAResponse
    {
        public Error Code { get; set; }

        public byte Subtract { get; set; }

        public byte ModVt { get; set; }

        public string Key { get; set; }

        public uint LinkNo { get; set; }

        public byte LinkType { get; set; }
    }
}
