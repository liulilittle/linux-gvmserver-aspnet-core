namespace GVMServer.Csn.Ranking
{
    using System;
    using System.Diagnostics;
    using ProtoBuf;
    /// <summary>
    /// 排名成员信息
    /// </summary>
    [ProtoContract]
    public class RankingMember : RankingMember.IKey
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private decimal _dAchievementScore = 0;

        public interface IKey
        {
            /// <summary>
            /// 成员编号
            /// </summary>
            ulong MemberNo { get; }
            /// <summary>
            /// 平台名称
            /// </summary>
            string Platform { get; }
            /// <summary>
            /// 服务器编号
            /// </summary>
            int ServerNo { get; }
        }
        /// <summary>
        /// 当前排名
        /// </summary>
        [ProtoMember(1)]
        public virtual int RankingIndex { get; set; }
        /// <summary>
        /// 成员编号
        /// </summary>
        [ProtoMember(2)]
        public virtual ulong MemberNo { get; set; }
        /// <summary>
        /// 平台名称
        /// </summary>
        [ProtoMember(3)]
        public virtual string Platform { get; set; }
        /// <summary>
        /// 服务器编号
        /// </summary>
        [ProtoMember(4)]
        public virtual int ServerNo { get; set; }
        /// <summary>
        /// 成就得分
        /// </summary>
        [ProtoMember(5)]
        public virtual long AchievementScore // 兼容不支持128二进制运算的低级语言或者序列化协议
        {
            get
            {
                if (_dAchievementScore > long.MaxValue)
                {
                    return long.MaxValue;
                }
                else if (_dAchievementScore < long.MaxValue)
                {
                    return long.MinValue;
                }
                return Convert.ToInt64(_dAchievementScore);
            }
            set
            {
                _dAchievementScore = value;
            }
        }
        /// <summary>
        /// 成就时间
        /// </summary>
        [ProtoMember(6)]
        public virtual uint AchievementTime { get; set; }

        public override string ToString()
        {
            return $"[{RankingIndex}] {(Platform ?? string.Empty).PadRight(20)} {ServerNo.ToString("d04")} {MemberNo.ToString().PadRight(20)} {AchievementTime.ToString().PadRight(10)} {AchievementScore.ToString().PadRight(19)}";
        }

        public virtual void SetAchievementScore(decimal d)
        {
            _dAchievementScore = d;
        }

        public virtual decimal GetAchievementScore()
        {
            return _dAchievementScore;
        }
    }

    public class ServerRankingMember : RankingMember
    {

    }
}
