namespace GVMServer.Csn.Protobuf
{
    using GVMServer.Ns.Enum;
    using ProtoBuf;

    [ProtoContract]
    public class RankingMemberInformationBulkUpdateResponse
    {
        /// <summary>
        /// 返回当前请求的操作错误状态
        /// </summary>
        [ProtoMember(1)]
        public Error Code { get; set; }
        /// <summary>
        /// 返回当前请求产生的事件计数(Nonquery)
        /// </summary>
        [ProtoMember(2)]
        public int Events { get; set; }
    }
}
