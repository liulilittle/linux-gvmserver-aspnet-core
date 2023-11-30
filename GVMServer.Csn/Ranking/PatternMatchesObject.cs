namespace GVMServer.Csn.Ranking
{
    using ProtoBuf;
    using System.Collections.Generic;

    /// <summary>
    /// 排名匹配表达式对象规则
    /// </summary>
    [ProtoContract]
    public class PatternMatchesObject<TRankingMember> where TRankingMember : RankingMember, new()
    {
        public static readonly IEnumerable<TRankingMember> EmptyMatches = new TRankingMember[0];
        /// <summary>
        /// 平台名称
        /// </summary>
        [ProtoMember(1)]
        public virtual string Platform { get; set; }
        /// <summary>
        /// 服务器编号
        /// </summary>
        [ProtoMember(2)]
        public virtual int ServerNo { get; set; }
        /// <summary>
        /// 成就得分
        /// </summary>
        [ProtoMember(3)]
        public virtual long AchievementScore { get; set; }
        /// <summary>
        /// 成就时间
        /// </summary>
        [ProtoMember(4)]
        public virtual uint AchievementTime { get; set; }
        /// <summary>
        /// 获取匹配规则字符串
        /// </summary>
        /// <returns></returns>
        public virtual string GetPatternKey() => LeaderboardAccessor<TRankingMember>.GetPatternKey(this);
    }
}
