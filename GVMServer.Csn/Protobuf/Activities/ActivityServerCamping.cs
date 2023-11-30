namespace GVMServer.Csn.Protobuf.Activities
{
    using ProtoBuf;

    [ProtoContract]
    public class ActivityServerCamping
    {
        /// <summary>
        /// 成员编号
        /// </summary>
        [ProtoMember(1)]
        public virtual ulong InstanceId { get; set; }
        /// <summary>
        /// 平台名称
        /// </summary>
        [ProtoMember(2)]
        public virtual string Platform { get; set; }
        /// <summary>
        /// 服务器编号
        /// </summary>
        [ProtoMember(3)]
        public virtual int ServerNo { get; set; }
    }
}
