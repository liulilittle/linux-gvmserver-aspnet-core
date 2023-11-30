namespace GVMServer.Csn.Ranking.Fundamental
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Threading;
    using Microsoft.Extensions.Configuration;

    public abstract class Leaderboard<TRankingMember> where TRankingMember : RankingMember, new() // 排行榜管理器
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IDictionary<int, LeaderboardObject<TRankingMember>> _globalLeaderboardObject = new ConcurrentDictionary<int, LeaderboardObject<TRankingMember>>();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IDictionary<string, IDictionary<int, LeaderboardObject<TRankingMember>>> _platformLeaderboardObject =
            new ConcurrentDictionary<string, IDictionary<int, LeaderboardObject<TRankingMember>>>();
        private readonly Timer _changeLeaderboardConfigurationWatchTimer = new Timer(1000);

        public virtual BaseApplication Application { get; }

        public Leaderboard(BaseApplication application)
        {
            this.Application = application ?? throw new ArgumentNullException(nameof(application));
            this.LoadAllLeaderboardObjectFromConfiguration();
            this._changeLeaderboardConfigurationWatchTimer.Tick += (sender, e) => this.LoadAllLeaderboardObjectFromConfiguration();
            this._changeLeaderboardConfigurationWatchTimer.Start();
        }

        private class LeaderboardObjectInitializeConfiguration
        {
            public string Platform { get; set; }

            public int RankingType { get; set; }

            public int Capacity { get; set; }
        }

        protected virtual void LoadAllLeaderboardObjectFromConfiguration()
        {
            IConfiguration configuration = this.GetConfiguration();
            foreach (IConfigurationSection kv in configuration.GetSection("PlatformRankingType").GetChildren() ?? new IConfigurationSection[0])
            {
                string platform = kv.Key;
                if (string.IsNullOrEmpty(platform))
                {
                    continue;
                }
                try
                {
                    LeaderboardObjectInitializeConfiguration[] s = kv.Get<LeaderboardObjectInitializeConfiguration[]>();
                    if (s == null || s.Length <= 0)
                    {
                        continue;
                    }
                    foreach (LeaderboardObjectInitializeConfiguration i in s)
                    {
                        lock (this._platformLeaderboardObject)
                        {
                            LeaderboardObject<TRankingMember> leaderboard = this.Get(platform, i.RankingType);
                            if (leaderboard != null)
                            {
                                continue;
                            }
                            leaderboard = this.New(platform, i.RankingType, i.Capacity);
                            if (leaderboard == null)
                            {
                                continue;
                            }
                            this.RegisterToPlatform(leaderboard);
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            do
            {
                try
                {
                    LeaderboardObjectInitializeConfiguration[] s = configuration.GetSection("GlobalRankingType").Get<LeaderboardObjectInitializeConfiguration[]>();
                    if (s == null || s.Length <= 0)
                    {
                        break;
                    }
                    foreach (LeaderboardObjectInitializeConfiguration i in s)
                    {
                        lock (this._globalLeaderboardObject)
                        {
                            LeaderboardObject<TRankingMember> leaderboard = this.Get(i.RankingType);
                            if (leaderboard != null)
                            {
                                continue;
                            }
                            leaderboard = this.New(i.RankingType, i.Capacity);
                            if (leaderboard == null)
                            {
                                continue;
                            }
                            this.RegisterToGlobal(leaderboard);
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            } while (false);
        }

        protected virtual IConfiguration GetConfiguration() => this.Application.GetConfiguration().GetSection("Leaderboard").GetSection("Fundamental");

        public virtual LeaderboardObject<TRankingMember> Get(string platform, int type)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return null;
            }
            lock (_platformLeaderboardObject)
            {
                _platformLeaderboardObject.TryGetValue(platform, out IDictionary<int, LeaderboardObject<TRankingMember>> d);
                if (d == null)
                {
                    return null;
                }
                d.TryGetValue(type, out LeaderboardObject<TRankingMember> r);
                return r;
            }
        }

        public virtual bool RegisterToPlatform(LeaderboardObject<TRankingMember> leaderboard)
        {
            if (leaderboard == null || string.IsNullOrEmpty(leaderboard.Platform))
            {
                return false;
            }
            lock (_platformLeaderboardObject)
            {
                _platformLeaderboardObject.TryGetValue(leaderboard.Platform, out IDictionary<int, LeaderboardObject<TRankingMember>> d);
                if (d == null)
                {
                    d = new ConcurrentDictionary<int, LeaderboardObject<TRankingMember>>();
                    d.Add(leaderboard.RankingType, leaderboard);
                    _platformLeaderboardObject[leaderboard.Platform] = d;
                    return true;
                }
                else
                {
                    d.TryGetValue(leaderboard.RankingType, out LeaderboardObject<TRankingMember> concurreis);
                    if (concurreis == null)
                    {
                        d[leaderboard.RankingType] = leaderboard;
                        return true;
                    }
                    else
                    {
                        return concurreis == leaderboard;
                    }
                }
            }
        }

        public virtual bool Unregister(string platform, int type)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return false;
            }
            lock (_platformLeaderboardObject)
            {
                _platformLeaderboardObject.TryGetValue(platform, out IDictionary<int, LeaderboardObject<TRankingMember>> d);
                if (d == null)
                {
                    return false;
                }
                bool than = d.Remove(type, out LeaderboardObject<TRankingMember> r);
                if (than && d.Count <= 0)
                {
                    _platformLeaderboardObject.Remove(platform);
                }
                return than;
            }
        }

        [SecurityCritical]
        public virtual LeaderboardObject<TRankingMember>[] GetAll([In]string platform, [In]int leaderboardType)
        {
            var management = this;
            if (management == null)
            {
                return new LeaderboardObject<TRankingMember>[0];
            }
            if (string.IsNullOrEmpty(platform))
            {
                var leaderboard = management.Get(leaderboardType);
                if (leaderboard == null)
                {
                    return new LeaderboardObject<TRankingMember>[0];
                }
                return new[] { leaderboard };
            }
            else
            {
                var leaderboards = new[]
                {
                    management.Get(string.Empty, leaderboardType),
                    management.Get(platform, leaderboardType)
                };
                int counts = 0;
                for (int i = 0; i < leaderboards.Length; i++)
                {
                    var leaderboard = leaderboards[i];
                    if (leaderboard != null)
                    {
                        counts++;
                    }
                }
                if (counts >= leaderboards.Length)
                {
                    return leaderboards;
                }
                var s = new LeaderboardObject<TRankingMember>[counts];
                for (int i = 0, j = 0; i < leaderboards.Length; i++)
                {
                    var leaderboard = leaderboards[i];
                    if (leaderboard == null)
                    {
                        continue;
                    }
                    s[j++] = leaderboard;
                    if (j >= counts)
                    {
                        break;
                    }
                }
                return s;
            }
        }

        public virtual LeaderboardObject<TRankingMember> Get(int globalType)
        {
            _globalLeaderboardObject.TryGetValue(globalType, out LeaderboardObject<TRankingMember> poLeaderboardObject);
            return poLeaderboardObject;
        }

        public virtual LeaderboardObject<TRankingMember> New(int globalType, int capacity)
        {
            return new LeaderboardObject<TRankingMember>(this.Application, globalType, capacity);
        }

        public virtual LeaderboardObject<TRankingMember> New(string platform, int type, int capacity)
        {
            return new LeaderboardObject<TRankingMember>(this.Application, platform, type, capacity);
        }

        public virtual LeaderboardObject<TRankingMember> Unregister(int globalType)
        {
            LeaderboardObject<TRankingMember> leaderboard = null;
            lock (_globalLeaderboardObject)
            {
                _globalLeaderboardObject.Remove(globalType, out leaderboard);
            }
            return leaderboard;
        }

        public virtual bool RegisterToGlobal(LeaderboardObject<TRankingMember> globalLeaderboard)
        {
            if (globalLeaderboard == null)
            {
                return false;
            }
            lock (_globalLeaderboardObject)
            {
                _globalLeaderboardObject.TryGetValue(globalLeaderboard.RankingType, out LeaderboardObject<TRankingMember> concurreis);
                if (concurreis == null)
                {
                    _globalLeaderboardObject[globalLeaderboard.RankingType] = globalLeaderboard;
                    return true;
                }
                else
                {
                    return concurreis == globalLeaderboard;
                }
            }
        }

        public virtual Error Matches(string platform, int rankingType, long minScore, long maxScore, int maxMatching, out IEnumerable<TRankingMember> matches)
        {
            return MatchesOffPlatform(platform, rankingType, minScore, maxScore, maxMatching, out matches, true);
        }

        public virtual Error MatchesOffPlatform(string platform, int rankingType, long minScore, long maxScore, int maxMatching, out IEnumerable<TRankingMember> matches)
        {
            return MatchesOffPlatform(platform, rankingType, minScore, maxScore, maxMatching, out matches, false);
        }

        private Error MatchesOffPlatform(string platform, int rankingType, long minScore, long maxScore, int maxMatching, out IEnumerable<TRankingMember> matches, bool offPlatform)
        {
            matches = null;
            LeaderboardObject<TRankingMember> leaderboard = null;
            if (string.IsNullOrEmpty(platform))
            {
                leaderboard = this.Get(rankingType);
            }
            else
            {
                leaderboard = this.Get(platform, rankingType);
            }

            if (leaderboard == null)
            {
                return Error.Error_UnableToFetchATheLeaderboardObjectInstance;
            }

            if (maxMatching <= 0)
            {
                maxMatching = -1;
            }

            PatternMatchesObject<TRankingMember> min = null;
            PatternMatchesObject<TRankingMember> max = null;
            if (0 != minScore)
            {
                min = new PatternMatchesObject<TRankingMember>()
                {
                    AchievementScore = minScore
                };
                if (!offPlatform)
                {
                    max.Platform = platform;
                }
            }

            if (0 != maxScore)
            {
                max = new PatternMatchesObject<TRankingMember>()
                {
                    AchievementScore = maxScore,
                };
                if (!offPlatform)
                {
                    max.Platform = platform;
                }
            }

            Error error = leaderboard.Sharding.Matches(min, max, maxMatching, out matches);
            if (error != Error.Error_Success)
            {
                return error;
            }

            return Error.Error_Success;
        }
    }
}
