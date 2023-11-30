namespace GVMServer.Net
{
    using System.Net;
    using System.Net.NetworkInformation;

    public class EthernetInterface
    {
        public NetworkInterface NetworkInterface { get; internal set; }

        public IPAddress UnicastAddresse { get; internal set; }

        public string MacAddress { get; internal set; }
    }
}
