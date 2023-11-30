namespace GVMServer.Csn.Protobuf
{
    using System.Collections.Generic;
    using GVMServer.Csn.Ranking;
    using GVMServer.Ns.Enum;
    using ProtoBuf;

    [ProtoContract]
    public class MatchRankingGamePlayersResponse
    {
        /// <summary>
        /// 返回的错误代码
        /// </summary>
        [ProtoMember(1)]
        public Error Code { get; set; }
        /// <summary>
        /// 匹配的结构
        /// </summary>
        [ProtoMember(2)]
        public IEnumerable<RankingMember> Matches { get; set; }
    }
}
