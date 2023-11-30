namespace GVMServer.Csn.Protobuf.Activities
{
    using ProtoBuf;

    [ProtoContract]
    public class PreopeningActivityResponse
    {
        [ProtoMember(1)]
        public int Code { get; set; }
    }
}
