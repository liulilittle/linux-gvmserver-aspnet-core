namespace GVMServer.Csn.Protobuf
{
    using System.Collections.Generic;
    using ProtoBuf;

    [ProtoContract]
    public class AllServerAverageRankingUpdateRequest
    {
        [ProtoContract]
        public class AllServerAverageRankingUpdateAverageValue
        {
            /// <summary>
            /// 请求参与当前排名成员列表批量更新的排行榜类型
            /// </summary>
            [ProtoMember(2)]
            public virtual int LeaderboardType { get; set; }
            /// <summary>
            /// 请求参与到指定排行榜类型的排名的成员数据集合
            /// </summary>
            [ProtoMember(3)]
            public virtual byte[] ScoreValue { get; set; }
        }
        /// <summary>
        /// 若为空平台则指定为全平台全区全服排名请求参与排名的排行榜
        /// </summary>
        [ProtoMember(1)]
        public virtual string Platform { get; set; }
        /// <summary>
        /// 平台服务区编号
        /// </summary>
        [ProtoMember(2)]
        public virtual int ServerNo { get; set; }
        /// <summary>
        /// 平均的值
        /// </summary>
        [ProtoMember(3)]
        public virtual IList<AllServerAverageRankingUpdateAverageValue> Values { get; set; }
    }
}
