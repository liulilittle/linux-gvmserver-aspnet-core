namespace GVMServer.Csn.Ranking
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using GVMServer.Linq;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net;
    using GVMServer.Threading;
    using Microsoft.Extensions.Configuration;

    public class LeaderboardObject<TRankingMember> where TRankingMember : RankingMember, new()
    {
        private IDictionary<string, TRankingMember> _rankingBulkUpdateList = null;
        private readonly object _syncobj = new object();
        private readonly Timer _rankingBulkUpdatePeriodsTimer = new Timer();
        private int _internalRankingType = 0;
        private int _cacehdFilePathAgingTicks = 0;
        private string _cacehdFilePath = string.Empty;
        /// <summary>
        /// 批量更新缓存文件数据键帧键
        /// </summary>
        private const byte BUCF_DFK = 0x2E;
        private const int PLATFROM_RANKING_TYPE_OFFSET = 100000;

        public string Platform { get; }

        public int Capacity { get; }

        public int RankingType { get; }

        public BaseApplication Application { get; }

        public LeaderboardObject(BaseApplication application, int type, int capacity) 
        {
            this.Application = application ?? throw new ArgumentNullException(nameof(application));
            this.RankingType = type;
            this._internalRankingType = type;
            this.Capacity = capacity;
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            this.Sharding = this.CreateSharding(type, 0, capacity);
            this._rankingBulkUpdateList = this.LoadFromBulkCachedFile(false);
            this._rankingBulkUpdatePeriodsTimer.Tick += this.OnBulkUpdate;
            this._rankingBulkUpdatePeriodsTimer.Interval = this.GetRankingBulkUpdatePeriods();
            this._rankingBulkUpdatePeriodsTimer.Start();
        }

        public LeaderboardObject(BaseApplication application, string platform, int type, int capacity) : this(application, type, capacity)
        {
            if (string.IsNullOrEmpty(platform))
            {
                throw new ArgumentOutOfRangeException("The platform used to instantiate the leaderboard object must not be null or an empty string");
            }
            this.Platform = platform;
            this.RankingType = type;
            this._internalRankingType = type + unchecked(string.IsNullOrEmpty(platform) ? 0 : PLATFROM_RANKING_TYPE_OFFSET);
        }

        protected virtual LeaderboardSharding<TRankingMember> CreateSharding(int sharding, int offset, int capacity)
        {
            return new LeaderboardSharding<TRankingMember>(this.CreateAccessor(), sharding, offset, offset + capacity);
        }

        protected virtual LeaderboardAccessor<TRankingMember> CreateAccessor()
        {
            return new LeaderboardAccessor<TRankingMember>();
        }

        protected virtual IDictionary<string, TRankingMember> LoadFromBulkCachedFile(bool truncate)
        {
            IDictionary<string, TRankingMember> results = new ConcurrentDictionary<string, TRankingMember>();
            try
            {
                FileStream fs = null;
                try
                {
                    string path = this.GetBulkCachedFilePath();
                    if (!File.Exists(path))
                    {
                        return results;
                    }
                    fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                }
                catch (Exception)
                {
                    return results;
                }
                using (fs)
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        try
                        {
                            while (fs.Position < fs.Length)
                            {
                                if (br.ReadByte() != BUCF_DFK)
                                {
                                    break;
                                }
                                int counts = br.ReadInt32();
                                if (counts <= 0)
                                {
                                    continue;
                                }
                                TRankingMember member = Encoding.UTF8.GetString(br.ReadBytes(counts)).FromJson<TRankingMember>();
                                if (member == null)
                                {
                                    continue;
                                }
                                this.BetterBulkAddOrUpdate(member, results);
                            }
                        }
                        catch (Exception) { }
                    }
                }
                return results;
            }
            finally
            {
                if (truncate)
                {
                    this.TruncateFromBulkCachedFile();
                }
            }
        }

        public static IConfiguration GetConfiguration(IConfiguration configuration) => configuration?.GetSection("Caches");

        protected virtual string GetCachedFileDirectories()
        {
            string cacehdFileDirectories = GetConfiguration(this.Application.GetConfiguration()).
                            GetSection("RankingUpdateListCachesDirectories").Get<string>();
            cacehdFileDirectories = Path.GetFullPath(cacehdFileDirectories);
            if (!Directory.Exists(cacehdFileDirectories))
            {
                if (!Directory.CreateDirectory(cacehdFileDirectories).Exists)
                {
                    cacehdFileDirectories = string.Empty;
                }
            }
            return cacehdFileDirectories;
        }

        protected virtual string GetBulkCachedFilePath()
        {
            Exception exception = null;
            lock (this._syncobj)
            {
                int currentticks = Environment.TickCount;
                int deltaticks = currentticks - this._cacehdFilePathAgingTicks;
                if (deltaticks < 0)
                {
                    deltaticks = -deltaticks;
                }
                if (deltaticks >= 1500)
                {
                    this._cacehdFilePathAgingTicks = currentticks;
                    try
                    {
                        string cacehdFileDirectories = this.GetCachedFileDirectories();
                        this._cacehdFilePath = $"ranking-bulk-update-list{(string.IsNullOrEmpty(this.Platform) ? string.Empty : "-" + this.Platform)}-{this._internalRankingType}-{typeof(TRankingMember).Name.ToLower()}.cache";
                        if (!string.IsNullOrEmpty(cacehdFileDirectories))
                        {
                            char ch = cacehdFileDirectories[cacehdFileDirectories.Length - 1];
                            if (ch == '\\' || ch == '/')
                            {
                                this._cacehdFilePath = cacehdFileDirectories + this._cacehdFilePath;
                            }
                            else
                            {
                                this._cacehdFilePath = cacehdFileDirectories + "/" + this._cacehdFilePath;
                            }
                            this._cacehdFilePath = this._cacehdFilePath.Replace('\\', '/'); // UNC格式
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
            }
            if (exception != null)
            {
                throw exception;
            }
            return this._cacehdFilePath;
        }

        protected virtual bool TruncateFromBulkCachedFile()
        {
            try
            {
                string path = this.GetBulkCachedFilePath();
                if (!File.Exists(path))
                {
                    return false;
                }
                using (FileStream fs = new FileStream(path,
                    FileMode.Truncate, FileAccess.ReadWrite))
                {
                    fs.Close();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected delegate void CachedFileWriter(TRankingMember member);

        protected virtual bool WritesBulkCachedFile(Action<CachedFileWriter> handling, out Exception exception)
        {
            exception = null;
            if (handling == null)
            {
                return false;
            }
            try
            {
                using (FileStream fs = new FileStream(this.GetBulkCachedFilePath(), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    fs.Seek(0, SeekOrigin.End);
                    try
                    {
                        using (BinaryWriter br = new BinaryWriter(fs))
                        {
                            CachedFileWriter cachedFileWriter = (m) =>
                            {
                                byte[] json = Encoding.UTF8.GetBytes(m.ToJson());
                                if (json.Any())
                                {
                                    br.Write(BUCF_DFK);
                                    br.Write(json.Length);
                                    br.Write(json);
                                }
                            };
                            try
                            {
                                handling(cachedFileWriter);
                                fs.Flush(); // 刷入缓存到磁盘文件簇
                            }
                            catch (Exception e)
                            {
                                exception = e;
                            }
                            cachedFileWriter = null;
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            return exception == null;
        }

        public virtual LeaderboardSharding<TRankingMember> Sharding { get; }

        protected virtual int GetRankingBulkUpdatePeriods() => 10000;

        protected virtual void OnBulkUpdate(object sender, EventArgs e)
        {
            bool garbageCollection = false;
            lock (this._syncobj)
            {
                IDictionary<string, TRankingMember> rankings = this._rankingBulkUpdateList; // 擦除排行榜等待更新的成员数据列表
                if (rankings.Any())
                {
                    this.Sharding.AddRankingMembers(rankings.Values, out Error error);
                    if (error == Error.Error_Success)
                    {
                        garbageCollection = true; // 要求GC以优化方式快速回收本次行为占用的大批量托管对象内存资源垃圾
                        this._rankingBulkUpdateList = null;
                        this.TruncateFromBulkCachedFile();
                    }
                }
            }
            if (garbageCollection)
            {
                GC.Collect();
            }
        }

        protected virtual IDictionary<string, TRankingMember> GetRankingBulkUpdateList()
        {
            lock (this._syncobj)
            {
                if (this._rankingBulkUpdateList == null)
                {
                    this._rankingBulkUpdateList = new ConcurrentDictionary<string, TRankingMember>();
                }
                return this._rankingBulkUpdateList;
            }
        }

        public bool AddBulkUpdate(TRankingMember m)
        {
            bool success = this.AddBulkUpdate(m, out Exception exception);
            if (exception != null)
            {
                throw exception;
            }
            return success;
        }

        public virtual bool AddBulkUpdate(TRankingMember m, out Exception exception)
        {
            exception = null;
            if (m == null || string.IsNullOrEmpty(m.Platform))
            {
                return false;
            }
            return this.AddBulkUpdate(new[] { m }, out exception) > 0;
        }

        protected virtual bool BetterBulkAddOrUpdate(TRankingMember member, IDictionary<string, TRankingMember> rankings)
        {
            if (member == null || string.IsNullOrEmpty(member.Platform))
            {
                return false;
            }
            string key = LeaderboardAccessor<TRankingMember>.GetMemberKey(member);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            rankings.TryGetValue(key, out TRankingMember currencies);
            if (!LeaderboardAccessor<TRankingMember>.Better(member, currencies))
            {
                return false;
            }
            else
            {
                rankings[key] = member;
            }
            return true;
        }

        public int AddBulkUpdate(IEnumerable<TRankingMember> s)
        {
            int events = this.AddBulkUpdate(s, out Exception exception);
            if (exception != null)
            {
                throw exception;
            }
            return events;
        }

        public virtual int AddBulkUpdate(IEnumerable<TRankingMember> s, out Exception exception)
        {
            int events = 0;
            exception = null;
            if (s.IsNullOrEmpty())
            {
                return events;
            }
            IDictionary<string, TRankingMember> rankings = this.GetRankingBulkUpdateList();
            lock (this._syncobj)
            {
                this.WritesBulkCachedFile((writer) =>
                {
                    foreach (TRankingMember member in s)
                    {
                        if (this.BetterBulkAddOrUpdate(member, rankings))
                        {
                            events++;
                            writer(member);
                        }
                    }
                }, out exception);
            }
            return events;
        }
    }
}
