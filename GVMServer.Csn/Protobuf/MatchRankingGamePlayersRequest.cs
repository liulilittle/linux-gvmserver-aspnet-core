namespace GVMServer.Csn.Protobuf
{
    using ProtoBuf;

    [ProtoContract]
    public class MatchRankingGamePlayersRequest
    {
        [ProtoContract]
        public class PatternTupleObject
        {
            /// <summary>
            /// 成就得分(不可缺省)
            /// </summary>
            [ProtoMember(3)]
            public virtual long AchievementScore { get; set; }
            /// <summary>
            /// 成就时间(最小为0)
            /// </summary>
            [ProtoMember(4)]
            public virtual uint AchievementTime { get; set; }
        }
        /// <summary>
        /// 排行榜类型
        /// </summary>
        [ProtoMember(1)]
        public virtual int LeaderboardType { get; set; }
        /// <summary>
        /// 最大匹配数量
        /// </summary>
        [ProtoMember(2)]
        public virtual int MaxNumberOfMatches { get; set; }
        /// <summary>
        /// 最小条件
        /// </summary>
        [ProtoMember(3)]
        public virtual PatternTupleObject Min { get; set; }
        /// <summary>
        /// 最大条件
        /// </summary>
        [ProtoMember(4)]
        public virtual PatternTupleObject Max { get; set; }
        /// <summary>
        /// 平台名称(可缺省；缺省为匹配多个平台成员)
        /// </summary>
        [ProtoMember(5)]
        public virtual string Platform { get; set; }
        /// <summary>
        /// 服务器编号(可缺省(0)；缺省为匹配多个服务器成员；通常设置为有值是没有意义的；但跨服集群计算节点允许这样的请求形势)
        /// </summary>
        [ProtoMember(6)]
        public virtual int ServerNo { get; set; }
    }
}
