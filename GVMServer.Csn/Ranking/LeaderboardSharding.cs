namespace GVMServer.Csn.Ranking
{
    using System;
    using System.Collections.Generic;
    using GVMServer.Linq;
    using GVMServer.Ns.Enum;
    using static GVMServer.Csn.Ranking.RankingMember;

    /// <summary>
    /// 联机分析排行榜(分片)
    /// </summary>
    public class LeaderboardSharding<TRankingMember> where TRankingMember : RankingMember, new()
    {
        /// <summary>
        /// 排行榜分片存储设备句柄（分布式的）
        /// </summary>
        public int Handle { get; }
        /// <summary>
        /// 分布式排行榜访问器（分布式的）
        /// </summary>
        public LeaderboardAccessor<TRankingMember> Accessor { get; }
        /// <summary>
        /// 分片起始SEEK
        /// </summary>
        public int ShardingStartSeek { get; }
        /// <summary>
        /// 分片结束SEEK
        /// </summary>
        public int ShardingEndingSeek { get; }
        /// <summary>
        /// 联机分析排行榜（分片）
        /// </summary>
        /// <param name="accessor">分布式排行榜访问器（分布式的）</param>
        /// <param name="handle">排行榜分片存储设备句柄(HSharding)</param>
        /// <param name="shardingStartSeek">分片起始Seek</param>
        /// <param name="shardingEndingSeek">分片结束Seek</param>
        public LeaderboardSharding(LeaderboardAccessor<TRankingMember> accessor, int handle, int shardingStartSeek, int shardingEndingSeek)
        {
            this.Accessor = accessor ?? throw new ArgumentNullException("An online analysis leaderboard accessor can be an null references");
            if (shardingStartSeek < 0)
                throw new ArgumentOutOfRangeException("sharding start seek cannot be less than zero");
            if (shardingEndingSeek <= 0)
                throw new ArgumentOutOfRangeException("sharding start seek cannot be less or equals than zero");
            if (shardingStartSeek >= shardingEndingSeek)
                throw new ArgumentOutOfRangeException("sharding start seek cannot be greater than or equal to sharding ending seek");
            if (handle < 0)
                throw new ArgumentOutOfRangeException("The sharded storage device handle is not allowed to be less than to 0");
            this.Handle = handle;
            this.ShardingStartSeek = shardingStartSeek;
            this.ShardingEndingSeek = shardingEndingSeek;
        }
        /// <summary>
        /// 获取最大成员数量
        /// </summary>
        /// <returns></returns>
        public int Capacity => this.ShardingEndingSeek - this.ShardingStartSeek;
        /// <summary>
        /// 获取指定排名位置的成员
        /// </summary>
        /// <param name="index">排名位置</param>
        /// <returns></returns>
        public virtual Error GetRankingMember(int index, out TRankingMember member) => this.Accessor.GetRankingMember(this.Handle, index, out member);
        /// <summary>
        /// 获取可用成员数量（分片托管的）
        /// </summary>
        /// <returns></returns>
        public virtual long GetRankingCount(out Error error) => this.Accessor.GetRankingCount(this.Handle, out error);
        /// <summary>
        /// 获取排名对应分片成员的索引
        /// </summary>
        /// <param name="rankingIndex">排名位置</param>
        /// <returns></returns>
        public virtual int GetShardingMemberIndex(int rankingIndex)
        {
            if (rankingIndex < this.ShardingStartSeek)
            {
                return ~0;
            }
            if (rankingIndex >= this.ShardingEndingSeek)
            {
                return ~0;
            }
            return rankingIndex - this.ShardingStartSeek;
        }
        /// <summary>
        /// 获取给定范围内的排名成员(仅限于此分片存储器内)
        /// </summary>
        /// <param name="index">排名起始索引</param>
        /// <param name="count">需要的数量(至少含1个)</param>
        /// <param name="s">返回检索到的排名成员列表</param>
        /// <returns></returns>
        public virtual Error GetRankingMembers(int index, int count, out List<TRankingMember> s) => this.Accessor.GetRankingMembers(this.Handle, index, count, out s);
        /// <summary>
        /// 检索全部可用排名成员数据(仅限于此分片存储器内）
        /// </summary>
        /// <param name="s">返回检索到的排名成员列表</param>
        /// <returns></returns>
        public virtual Error GetAllRankingMembers(out List<TRankingMember> s) => this.Accessor.GetAllRankingMembers(this.Handle, out s);
        /// <summary>
        /// 添加或者更新排名成员(仅限于此分片存储内)
        /// </summary>
        /// <param name="member">参与到本分片添加或者更新计算的排名成员信息</param>
        /// <param name="error">返回当前操作的错误代码</param>
        /// <returns></returns>
        public virtual bool AddRankingMember(TRankingMember member, out Error error) => this.Accessor.AddRankingMember(this.Handle, this.Capacity, member, out error);
        /// <summary>
        /// 添加或更新多个成员的排名(仅限于此分片存储内)
        /// </summary>
        /// <param name="members">参与到本分片添加或者更新计算的排名成员信息列表</param>
        /// <param name="error">返回当前操作的错误代码</param>
        /// <returns></returns>
        public virtual int AddRankingMembers(IEnumerable<TRankingMember> members, out Error error)
        {
            int events = 0;
            error = Error.Error_Success;
            if (members.IsNullOrEmpty())
                return events;
            error = this.Accessor.AddRankingMembers(this.Handle, this.Capacity, members, out events);
            return events;
        }
        /// <summary>
        /// 删除本分片已经上榜的成员的信息
        /// </summary>
        /// <param name="s">需要删除的从本分片之中上榜的排名成员</param>
        /// <param name="error">返回当前操作的错误代码</param>
        /// <returns></returns>
        public virtual int RemoveAllRankingMembers(IEnumerable<IKey> s, out Error error)
        {
            int events = 0;
            error = Error.Error_Success;
            if (s.IsNullOrEmpty())
                return events;
            error = this.Accessor.RemoveAllRankingMembers(this.Handle, s, out events);
            return events;
        }
        /// <summary>
        /// 删除本分片已经上榜的成员信息
        /// </summary>
        /// <param name="key">需要删除的从本分片之中上榜的排名成员</param>
        /// <param name="error">返回当前操作的错误代码</param>
        /// <returns></returns>
        public virtual bool RemoveRankingMember(IKey key, out Error error) => this.Accessor.RemoveRankingMember(this.Handle, key, out error);
        /// <summary>
        /// 彻底清空排行榜序列集合的排名数据
        /// </summary>
        /// <returns></returns>
        public virtual Error Clear() => this.Accessor.Clear(this.Handle);
        /// <summary>
        /// 按既定条件匹配排名的对象数据
        /// </summary>
        /// <param name="min">最小条件</param>
        /// <param name="max">最大条件</param>
        /// <param name="length">匹配个数</param>
        /// <param name="s">返回匹配的结果</param>
        /// <returns></returns>
        protected virtual Error Matches(string min, string max, int length, out IEnumerable<TRankingMember> s) => this.Accessor.Matches(this.Handle, min, max, length, out s);
        /// <summary>
        /// 按既定条件匹配排名的对象数据
        /// </summary>
        /// <param name="min">最小条件</param>
        /// <param name="max">最大条件</param>
        /// <param name="length">匹配个数</param>
        /// <param name="s">返回匹配的结果</param>
        /// <returns></returns>
        public virtual Error Matches(PatternMatchesObject<TRankingMember> min, PatternMatchesObject<TRankingMember> max, int length, out IEnumerable<TRankingMember> s)
        {
            if (length < 0)
                length = -1;
            else if (length == 0)
                length = 1;
            string minPatternKey = string.Empty; // "-" negative 阴
            string maxPatternKey = string.Empty; // "+" positive 阳
            if (min != null)
                minPatternKey = min.GetPatternKey();
            if (max != null)
                maxPatternKey = max.GetPatternKey();
            return this.Matches(minPatternKey, maxPatternKey, length, out s);
        }
    }
}
