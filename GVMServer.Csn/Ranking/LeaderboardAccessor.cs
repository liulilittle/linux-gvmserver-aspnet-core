namespace GVMServer.Csn.Ranking
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using GVMServer.Linq;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using ServiceStack.Redis;
    using ServiceStack.Redis.Pipeline;
    using static GVMServer.Csn.Ranking.RankingMember;
    using ILeaderboardShardingStorage = ServiceStack.Redis.IRedisClient;

    /// <summary>
    /// 排名分片存储器(分布式的)
    /// </summary>
    /// <typeparam name="TRankingMember">排名成员类型</typeparam>
    public class LeaderboardAccessor<TRankingMember> where TRankingMember : RankingMember, new()
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string RankingClassNameLower = typeof(TRankingMember).Name.ToLower();

        public static string GetSortedSetKey(int sharding)
        {
            string key = $"ns.computenode.leaderboard.scoretimeset.{RankingClassNameLower}.{sharding}";
            return key;
        }

        public static string GetMemberKey(IKey key)
        {
            if (key == null)
            {
                return string.Empty;
            }
            return $"ns.computenode.leaderboard.rankingmember.data.{RankingClassNameLower}.{key.Platform}.{key.ServerNo}.{key.MemberNo}";
        }

        public static string GetPatternKey(PatternMatchesObject<TRankingMember> pattern)
        {
            if (pattern == null)
                return string.Empty;
            string patternkey = $"[ns.computenode.leaderboard.rankingmember.sortedkey->{RankingClassNameLower}.{pattern.AchievementScore}.{pattern.AchievementTime}";
            if (!string.IsNullOrEmpty(pattern.Platform))
            {
                patternkey += string.Format(".{0}", pattern.Platform);
                if (0 != pattern.ServerNo)
                    patternkey += string.Format(".{0}", pattern.ServerNo);
            }
            return patternkey;
        }

        public static string GetSortedKey(TRankingMember member)
        {
            if (member == null)
                return string.Empty;
            string joinkey = $"ns.computenode.leaderboard.rankingmember.sortedkey->{RankingClassNameLower}.{member.AchievementScore}.{member.AchievementTime}.{member.Platform}.{member.ServerNo}.{member.MemberNo}";
            return joinkey;
        }

        public static string GetSynchronizeKey(int sharding)
        {
            string key = $"ns.computenode.leaderboard.scoretimeset.syncobj.{RankingClassNameLower}.{sharding}";
            return key;
        }

        public static TRankingMember DisassembleSortedKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            int index = key.IndexOf("->");
            if (index < 0)
                return null;
            string[] s = key.Substring(index + 2).Split('.');
            if (s.IsNullOrEmpty())
                return null;
            TRankingMember member = new TRankingMember
            {
                AchievementScore = Convert.ToInt64(s[1]),
                AchievementTime = Convert.ToUInt32(s[2]),
                Platform = s[3],
                ServerNo = Convert.ToInt32(s[4]),
                MemberNo = Convert.ToUInt64(s[5])
            };
            return member;
        }

        private static Error FetchShardingStorage(int sharding, Func<ILeaderboardShardingStorage, Error> handling, bool writing = true) => FetchShardingStorage(sharding, handling, 10, writing);

        private static Error FetchShardingStorage(int sharding, Func<ILeaderboardShardingStorage, Error> handling, int timeout, bool writing)
        {
            if (!writing)
                return CacheAccessor.GetClient((storage) => handling(storage));
            return CacheAccessor.GetClient((storage) => CacheAccessor.AcquireLock(storage, GetSynchronizeKey(sharding), () => handling(storage), timeout));
        }

        #region 公共接口
        public virtual Error GetRankingMember(int sharding, int index, out TRankingMember member)
        {
            TRankingMember ranking = default(TRankingMember);
            Error error = FetchShardingStorage(sharding, (storage) => GetRankingMember(storage, sharding, index, out ranking), false);
            member = ranking;
            return error;
        }

        public virtual long GetRankingCount(int sharding, out Error error)
        {
            long rankingcount = 0;
            error = FetchShardingStorage(sharding, (storage) => GetRankingCount(storage, sharding, out rankingcount), false);
            return rankingcount;
        }

        public virtual Error GetRankingMembers(int sharding, int index, int length, out List<TRankingMember> s)
        {
            List<TRankingMember> r = null;
            try
            {
                return FetchShardingStorage(sharding, (storage) => GetRankingMembers(storage, sharding, index, length, out r), false);
            }
            finally
            {
                s = r;
            }
        }

        public virtual Error GetAllRankingMembers(int sharding, out List<TRankingMember> s) => GetRankingMembers(sharding, 0, -1, out s);

        public virtual bool AddRankingMember(int sharding, int capacity, TRankingMember member, out Error error)
        {
            error = Error.Error_Success;
            if (member == null)
            {
                error = Error.Error_YourInputTheRankingMemberIsNullReferences;
                return false;
            }
            return (error = FetchShardingStorage(sharding, (storage) => AddRankingMember(storage, sharding, capacity, member))) == Error.Error_Success;
        }

        public virtual TRankingMember GetRankingMemberIndex(int sharding, int capacity, IKey key, out Error error)
        {
            error = Error.Error_Success;
            if (key == null)
                return default(TRankingMember);
            error = GetRankingMemberIndexes(sharding, capacity, new[] { key }, out List<TRankingMember> indexes);
            if (error != Error.Error_Success || indexes == null)
                return default(TRankingMember);
            return indexes.FirstOrDefault();
        }

        public virtual Error GetRankingMemberIndexes(int sharding, int capacity, IEnumerable<IKey> s, out List<TRankingMember> indexes)
        {
            indexes = null;
            if (s.IsNullOrEmpty())
                return Error.Error_Success;
            List<TRankingMember> results = null;
            Error error = FetchShardingStorage(sharding, (storage) => GetRankingMemberIndexes(storage, sharding, capacity, s, out results), false);
            indexes = results;
            return error;
        }

        public virtual Error AddRankingMembers(int sharding, int capacity, IEnumerable<TRankingMember> s, out int events)
        {
            events = 0;
            if (s.IsNullOrEmpty())
                return Error.Error_Success;
            int EE = 0;
            Error error = FetchShardingStorage(sharding, (storage) => AddRankingMembers(storage, sharding, capacity, s, out EE));
            events = EE;
            return error;
        }

        public virtual bool RemoveRankingMember(int sharding, IKey key, out Error error)
        {
            error = Error.Error_Success;
            if (key == null)
            {
                error = Error.Error_YourInputTheRankingMemberIsNullReferences;
                return false;
            }
            error = RemoveAllRankingMembers(sharding, new[] { key }, out int events);
            return error == Error.Error_Success;
        }

        public virtual Error RemoveAllRankingMembers(int sharding, IEnumerable<IKey> keys, out int events)
        {
            events = 0;
            if (keys.IsNullOrEmpty())
                return Error.Error_Success;
            int EE = 0;
            Error error = FetchShardingStorage(sharding, (storage) => RemoveAllRankingMembers(storage, sharding, keys, out EE));
            events = EE;
            return error;
        }

        public virtual Error Matches(int sharding, string min, string max, int length, out IEnumerable<TRankingMember> s)
        {
            IEnumerable<TRankingMember> RS = null;
            Error error = FetchShardingStorage(sharding, (storage) => Matches(storage, sharding, min, max, length, out RS), false);
            s = RS;
            return error;
        }

        public virtual Error Clear(int sharding)
        {
            return FetchShardingStorage(sharding, (storage) => this.Clear(storage, sharding));
        }
        #endregion

        #region 可里氏代换的内部保护函数
        protected virtual Error GetAllRankingMembers(ILeaderboardShardingStorage storage, IEnumerable<string> keys, out IDictionary<string, TRankingMember> s) => CacheAccessor.GetValues(storage, keys, out s);

        protected virtual Error GetRankingMember(ILeaderboardShardingStorage storage, int sharding, int index, out TRankingMember member)
        {
            member = default(TRankingMember);
            if (storage == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            Error error = GetRankingMembers(storage, sharding, index, 1, out List<TRankingMember> s);
            if (s != null && s.Count > 0)
            {
                member = s[0];
            }
            return error;
        }

        protected virtual Error GetRankingMembers(ILeaderboardShardingStorage storage, int sharding, int index, int length, out List<TRankingMember> s)
        {
            s = null;
            try
            {
                Error error = CacheAccessor.GetRangeFromSortedSetDesc(storage, GetSortedSetKey(sharding), index, -1, out List<string> roles);
                if (error != Error.Error_Success)
                {
                    return error;
                }
                error = GetAllRankingMembers(storage, roles.Conversion(i => GetMemberKey(DisassembleSortedKey(i))), out IDictionary<string, TRankingMember> key2members);
                if (error != Error.Error_Success)
                {
                    return error;
                }
                s = new List<TRankingMember>();
                if (key2members != null)
                {
                    int offset = index;
                    foreach (TRankingMember m in key2members.Values)
                    {
                        if (m == null)
                            continue;
                        m.RankingIndex = offset++;
                        s.Add(m);
                    }
                }
            }
            catch (Exception)
            {
                return Error.Error_ThereWasAnUnexpectedProblemGettingRankingMemberForASliceFromTheShardingStore;
            }
            return Error.Error_Success;
        }

        protected virtual Error GetRankingCount(ILeaderboardShardingStorage storage, int sharding, out long count) => CacheAccessor.GetSortedSetCount(storage, GetSortedSetKey(sharding), out count);

        protected virtual Error AddRankingMember(ILeaderboardShardingStorage storage, int sharding, int capacity, TRankingMember member)
        {
            /* 
             * (分数+时间[秒])+(平台+服务器)+角色 | 按照我们的排行榜会遇到诡异数值系统设计破千万亿(1e+16 + ~0)，这样子设计序列键比较合理（可容纳万人流式实时排名；）
             *      但其具有固有缺陷即：同分同时高平台编码会优先排列在前[当然这到也并不是什么大问题]
             * (分数+时间) ->【跳表或跳集】 -> (成员集合) 的结构在访问大量成员数据量时会有I/O瓶颈问题
             */
            if (member == null)
                return Error.Error_YourInputTheRankingMemberIsNullReferences;
            string kElement = GetMemberKey(member); // 当前成员
            Error error = CacheAccessor.GetValue(storage, kElement, out TRankingMember current);
            if (error != Error.Error_Success)
                return error;
            if ((error = this.GetRankingCount(storage, sharding, out long counts)) != Error.Error_Success)
                return error;
            IRedisTransaction transaction = null;
            do
            {
                string kSet = GetSortedSetKey(sharding);
                if (counts >= capacity) // 若当前参与排名成员的数量小于总容量时
                {
                    TRankingMember evaluation = null;
                    do
                    {
                        if (current != null)
                        {
                            long index = CacheAccessor.GetItemIndexInSortedSetDesc(storage, kSet, GetSortedKey(current), out error);
                            if (error != Error.Error_Success)
                                break;
                            evaluation = current;
                            if (index >= 0 && index < capacity)
                                break;
                        }
                        if ((error = CacheAccessor.GetRangeFromSortedSetDesc(storage, kSet, Convert.ToInt32(counts - 1), -1, out List<string> worsts)) != Error.Error_Success)
                            break;
                        evaluation = DisassembleSortedKey(worsts.FirstOrDefault());
                    } while (false);
                    if (evaluation == null)
                    {
                        error = Error.Error_UnableToGetLastRankingMemberElement;
                        break;
                    }
                    else if (Better(evaluation, member))
                        return Error.Error_Success;
                    else
                    {
                        if (transaction == null)
                            if ((transaction = CacheAccessor.CreateTransaction(storage, out error)) == null)
                                break;
                        transaction.QueueCommand(r => r.Remove(GetMemberKey(evaluation))); // 删除现在排行榜上的最后一个人的信息
                        transaction.QueueCommand(r => r.RemoveItemFromSortedSet(kSet, GetSortedKey(evaluation)));
                    }
                }
                else if (current != null)
                {
                    if (current.AchievementScore > member.AchievementScore ||
                            (current.AchievementScore == member.AchievementScore && member.AchievementTime >= current.AchievementTime))
                        return Error.Error_Success;
                }
                if (error == Error.Error_Success)
                {
                    if (transaction == null)
                        if ((transaction = CacheAccessor.CreateTransaction(storage, out error)) == null)
                            break;
                    transaction.QueueCommand(r => r.Set(kElement, member)); // 添加未被存储的成员
                    transaction.QueueCommand(r => r.AddItemToSortedSet(kSet, GetSortedKey(member))); // 添加或者更新成员排名
                }
            } while (false);
            return CacheAccessor.CommitedTransaction(transaction, error);
        }

        public static bool Better(TRankingMember x, TRankingMember y)
        {
            if (x == y)
                return true;
            if (x != null && y == null)
                return true;
            else if (y == null)
                return false;
            return x.AchievementScore > y.AchievementScore ||
                        (x.AchievementScore == y.AchievementScore && y.AchievementTime > x.AchievementTime);
        }

        protected virtual Error AddRankingMembers(ILeaderboardShardingStorage storage, int sharding, int capacity, IEnumerable<TRankingMember> members, out int events)
        {
            Error error = Error.Error_Success;
            events = 0;
            try
            {
                if (members.IsNullOrEmpty())
                    return Error.Error_Success;
                string kSet = GetSortedSetKey(sharding);
                var elements = new Dictionary<string, TRankingMember>();
                foreach (var i in members)
                {
                    if (i == null)
                        continue;
                    var k = GetMemberKey(i);
                    if (!elements.TryGetValue(k, out TRankingMember max))
                        elements.Add(k, i);
                    else if (max == null)
                        elements[k] = i;
                    else if (Better(i, max)) // 具有重复添加的成员则比较两者之间的成就时间与成就得分
                    {
                        events++;
                        elements[k] = i;
                    }
                }
                error = CacheAccessor.GetValues(storage, elements.Keys, out IDictionary<string, TRankingMember> currencies);
                if (error != Error.Error_Success)
                    return error;
                var transaction = CacheAccessor.CreateTransaction(storage, out error);
                if (error != Error.Error_Success)
                    return error;
                var many = false;
                foreach (var kv in elements)
                {
                    var i = kv.Value;
                    if (!many)
                    {
                        many = true;
                        elements = new Dictionary<string, TRankingMember>();
                    }
                    string kEl = GetMemberKey(i);
                    if (currencies != null)
                    {
                        currencies.TryGetValue(kEl, out TRankingMember current);
                        if (current != null)
                        {
                            if (current.AchievementScore > i.AchievementScore || (current.AchievementScore == i.AchievementScore && i.AchievementTime >= current.AchievementTime))
                            {
                                events++;
                                continue;
                            }
                            transaction.QueueCommand(r => r.RemoveItemFromSortedSet(kSet, GetSortedKey(current)));
                        }
                    }
                    events++;
                    elements.Add(kEl, i);
                    transaction.QueueCommand(r => r.AddItemToSortedSet(kSet, GetSortedKey(i)));
                }
                if (elements.Count > 0)
                    transaction.QueueCommand(r => r.SetAll(elements));
                if (error != Error.Error_Success || events <= 0)
                    return CacheAccessor.RollbackTransaction(transaction, error);
                if ((error = CacheAccessor.CommitedTransaction(transaction, error)) != Error.Error_Success)
                    return error;
                error = CacheAccessor.GetRangeFromSortedSetDesc(storage, kSet, capacity, -1, out List<string> overflowSortedKeys);
                if (error != Error.Error_Success || !overflowSortedKeys.Any())
                    return error;
                var pipeline = CacheAccessor.CreatePipeline(storage, out error);
                if (pipeline == null || error != Error.Error_Success)
                    return error;
                else
                {
                    pipeline.QueueCommand(r => r.RemoveItemsFromSortedSet(kSet, overflowSortedKeys));
                    pipeline.QueueCommand(r => r.RemoveAll(overflowSortedKeys.Conversion(i => GetMemberKey(DisassembleSortedKey(i)))));
                }
                error = CacheAccessor.FlushPipeline(pipeline, error);
                return error;
            }
            finally
            {
                events = error != Error.Error_Success ? 0 : events;
            }
        }

        protected virtual Error RemoveAllRankingMembers(ILeaderboardShardingStorage storage, int sharding, IEnumerable<IKey> keys, out int events)
        {
            events = 0;
            if (keys.IsNullOrEmpty())
                return Error.Error_Success;
            Error error = Error.Error_Success;
            if (storage == null)
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            var deleteMembersSet = new List<string>();
            var deleteMembers = new List<string>();
            var deleteMembersSetImpl = new HashSet<string>();
            foreach (var member in keys)
            {
                if (member == null)
                    continue;
                if (!string.IsNullOrEmpty(member.Platform))
                    if (member is TRankingMember i)
                    {
                        if (string.IsNullOrEmpty(i.Platform))
                            continue;
                        string sk = GetSortedKey(i);
                        if (!string.IsNullOrEmpty(sk))
                        {
                            if (!deleteMembersSetImpl.Add(sk))
                                continue;
                            deleteMembersSet.Add(sk);
                        }
                    }
                deleteMembers.Add(GetMemberKey(member));
            }
            if (deleteMembers.Count <= 0)
                return Error.Error_Success;
            error = CacheAccessor.GetValues(storage, deleteMembers, out IDictionary<string, TRankingMember> deleteMemberDictionary);
            if (error != Error.Error_Success)
                return error;
            if (deleteMemberDictionary.Any())
            {
                foreach (var i in deleteMemberDictionary.Values)
                {
                    if (i == null)
                        continue;
                    string sk = GetSortedKey(i);
                    if (!string.IsNullOrEmpty(sk))
                    {
                        if (!deleteMembersSetImpl.Add(sk))
                            continue;
                        deleteMembersSet.Add(sk);
                    }
                }
            }
            IRedisTransaction transaction = CacheAccessor.CreateTransaction(storage, out error);
            if (error != Error.Error_Success)
                return error;
            long counts = 0;
            try
            {
                transaction.QueueCommand(r => r.RemoveItemsFromSortedSet(GetSortedSetKey(sharding), deleteMembersSet));
                transaction.QueueCommand(r => r.RemoveAll(deleteMembers));
                counts = deleteMembersSet.Count;
                error = CacheAccessor.CommitedTransaction(transaction, error);
            }
            catch (Exception)
            {
                error = CacheAccessor.RollbackTransaction(transaction, error);
            }
            events = (int)(error != Error.Error_Success ? 0 : counts);
            return error;
        }

        protected virtual Error GetRankingMemberIndexes(ILeaderboardShardingStorage storage, int sharding, int capacity, IEnumerable<IKey> keys, out List<TRankingMember> s)
        {
            s = null;
            if (keys.IsNullOrEmpty())
                return Error.Error_Success;
            Error error = CacheAccessor.GetValues(storage, keys.Conversion(i => GetMemberKey(i)), out IDictionary<string, TRankingMember> sm);
            if (error != Error.Error_Success)
                return error;
            else
                s = new List<TRankingMember>();
            if (sm != null)
            {
                string kSet = GetSortedSetKey(sharding);
                foreach (var m in sm.Values)
                {
                    string k = GetSortedKey(m);
                    if (string.IsNullOrEmpty(k))
                        continue;
                    int index = unchecked((int)CacheAccessor.GetItemIndexInSortedSetDesc(storage, kSet, k, out error));
                    if (error != Error.Error_Success)
                        break;
                    if (capacity >= 0 && index >= capacity)
                        index = ~0;
                    m.RankingIndex = index;
                    s.Add(m);
                }
            }
            return Error.Error_Success;
        }

        protected virtual Error Clear(ILeaderboardShardingStorage storage, int sharding)
        {
            if (storage == null)
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            string kSet = GetSortedSetKey(sharding);
            Error error = CacheAccessor.GetRangeFromSortedSetDesc(storage, kSet, 0, -1, out List<string> s);
            if (error != Error.Error_Success || s.IsNullOrEmpty())
                return error;
            IRedisPipeline pipeline = CacheAccessor.CreatePipeline(storage, out error);
            if (error != Error.Error_Success)
                return CacheAccessor.FlushPipeline(pipeline, error);
            else
            {
                pipeline.QueueCommand(r => CacheAccessor.Remove(r, kSet));
                pipeline.QueueCommand(r => CacheAccessor.RemoveAll(r, s.Conversion(i => GetMemberKey(DisassembleSortedKey(i)))));
            }
            return CacheAccessor.FlushPipeline(pipeline, error);
        }

        protected virtual Error Matches(ILeaderboardShardingStorage storage, int sharding, string min, string max, int length, out IEnumerable<TRankingMember> s)
        {
            s = null;
            if (string.IsNullOrEmpty(min))
                min = "-";
            if (string.IsNullOrEmpty(max))
                max = "+";
            Error error = CacheAccessor.ZRevrangeBylex(storage, GetSortedSetKey(sharding), min, max, 0, -1, out IEnumerable<string> sortedkeys);
            if (sortedkeys != null)
                s = sortedkeys.Conversion(i => DisassembleSortedKey(i));
            return error;
        }
        #endregion
    }
}