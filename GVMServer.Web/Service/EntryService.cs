namespace GVMServer.Web.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using GVMServer.Cache;
    using GVMServer.DDD.Service;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Net;
    using GVMServer.Web.Database;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using ServiceStack.Redis;

    public class EntryService : IEntryService
    {
        public const string DEFAULT_PLATFORM = XiYouSdkClient.DEFAULT_PLATFORM;
        private const string ENTRY_KEY_NAME = "entry.init";
        private const string SERVER_DICT_KEY_NAME = "servers.dict";
        private const string SERVER_LIST_KEY_NAME = "servers.init";

        private class CacheContent
        {
            public JToken jToken;
            public string szJson;
            public string szJsonRepr;
            public string szBindKey;
            public string szPlatform;
            public string szPaid;
            public DateTime dtLastTime;
            public DateTime dtTickTime;
            public EntryCacheCategories eEcc;
        };

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<EntryCacheCategories, CacheContent>> m_oCacheContents
            = new ConcurrentDictionary<string, ConcurrentDictionary<EntryCacheCategories, CacheContent>>();
        private volatile bool m_disposed = false;
        private readonly Thread m_oWorkThread = null;

        public EntryService()
        {
            m_oWorkThread = new Thread(() =>
            {
                Stopwatch pReprepareStopwatch = new Stopwatch();
                pReprepareStopwatch.Restart();

                PrepareAsync(null).Wait();
                PreserveEntities(() =>
                {
                    TimeSpan tsRepreparElapsed = pReprepareStopwatch.Elapsed;
                    if (tsRepreparElapsed.TotalSeconds >= 10)
                    {
                        PrepareAsync(null);
                        pReprepareStopwatch.Restart();
                    }
                });
            });
            m_oWorkThread.IsBackground = true;
            m_oWorkThread.Priority = ThreadPriority.Lowest;
            m_oWorkThread.Start();
        }

        private void PreserveEntities(Action tick)
        {
            DateTime dateTime = DateTime.Now;
            IList<string> keys = new List<string>();
            IList<CacheContent> contents = new List<CacheContent>();
            while (!m_disposed)
            {
                keys.Clear();
                contents.Clear();
                dateTime = DateTime.Now;
                foreach (var kv in m_oCacheContents)
                {
                    foreach (CacheContent content in kv.Value.Values)
                    {
                        if (string.IsNullOrEmpty(content.szBindKey))
                        {
                            continue;
                        }

                        if (dateTime.Subtract(content.dtLastTime).TotalSeconds >= 5)
                        {
                            keys.Add(content.szBindKey);
                            contents.Add(content);
                            content.dtLastTime = dateTime;
                        }

                        if (dateTime.Subtract(content.dtTickTime).TotalSeconds >= 1)
                        {
                            content.dtTickTime = dateTime;
                            if (content.eEcc == EntryCacheCategories.EntryPoint)
                            {
                                JToken jToken = content.jToken;
                                if (jToken != null)
                                {
                                    jToken["server_info"]["server_time"] = XiYouUtility.ToTimeSpan10(DateTime.Now);
                                    content.szJsonRepr = jToken.ToString(Formatting.None);
                                }
                            }
                        }
                    }
                }

                if (keys.Count > 0)
                {
                    TryGetRedisClient((redis) =>
                    {
                        var dict = redis.GetAll<string>(keys);
                        var index = 0;
                        foreach (var kv in dict)
                        {
                            string json = kv.Value;
                            CacheContent content = contents[index++];
                            if (json != content.szJson)
                            {
                                content.szJson = json;
                                content.jToken = JToken.Parse(json);
                            }
                        }
                    });
                }

                tick?.Invoke();

                Thread.Sleep(500);
            }
        }

        private Task PrepareAsync(Action callback)
        {
            var task = new Task(() =>
            {
                DateTime dateTime = DateTime.Now;
                IDictionary<string, HashSet<string>> entities = null;
                while (!m_disposed)
                {
                    bool completed = false;
                    do
                    {
                        IEntryGateway gateway = ServiceObjectContainer.Get<IEntryGateway>();
                        if (gateway == null)
                        {
                            break;
                        }

                        if (entities == null)
                        {
                            entities = gateway.GetAllEntities();
                        }

                        if (entities == null && entities.Count <= 0)
                        {
                            break;
                        }

                        ISet<string> demandrefreshs = new HashSet<string>();
                        TryGetRedisClient((redis) =>
                        {
                            foreach (var pair in entities)
                            {
                                string platform = pair.Key;
                                foreach (var paid in pair.Value)
                                {
                                    if (string.IsNullOrEmpty(platform))
                                    {
                                        continue;
                                    }

                                    string keyEntry = GetEntryKey(platform, paid);
                                    string keyServersDict = GetServersDictKey(platform, paid);
                                    string keyServerList = GetServersListKey(platform, paid);

                                    if (!redis.ContainsKey(keyEntry) ||
                                        !redis.ContainsKey(keyServersDict) ||
                                        !redis.ContainsKey(keyServerList))
                                    {
                                        demandrefreshs.Add(platform);
                                    }

                                    AddCacheContentRef(platform, paid, EntryCacheCategories.EntryPoint, keyEntry);
                                    AddCacheContentRef(platform, paid, EntryCacheCategories.ServersDictionary, keyServersDict);
                                    AddCacheContentRef(platform, paid, EntryCacheCategories.ServersList, keyServerList);
                                }
                            }
                        });
                        entities.Clear();
                        foreach (string platform in demandrefreshs)
                        {
                            Refresh(platform);
                        }
                        completed = true;
                    } while (false);
                    if (completed || DateTime.Now.Subtract(dateTime).TotalSeconds >= 10)
                    {
                        callback?.Invoke();
                        break;
                    }
                    Thread.Sleep(1000);
                }
            });
            task.Start();
            return task;
        }

        ~EntryService()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!m_disposed)
                {
                    m_disposed = true;
                    m_oCacheContents.Clear();
                }
            }
            GC.SuppressFinalize(this);
        }

        private static string GetCacheContentKeyRef(string platform, string paid)
        {
            if (!string.IsNullOrEmpty(platform))
            {
                platform = platform.TrimStart().TrimEnd();
            }
            if (!string.IsNullOrEmpty(paid))
            {
                paid = paid.TrimStart().TrimEnd();
                if (paid == "0")
                {
                    paid = string.Empty;
                }
            }
            string key = platform;
            if (!string.IsNullOrEmpty(paid))
            {
                key += "." + paid;
            }
            return key;
        }

        private CacheContent AddCacheContentRef(string platform, string paid, EntryCacheCategories categories, string bindkey)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return null;
            }
            string key = GetCacheContentKeyRef(platform, paid);
            lock (m_oCacheContents)
            {
                ConcurrentDictionary<EntryCacheCategories, CacheContent> d = null;
                if (!m_oCacheContents.TryGetValue(key, out d))
                {
                    d = new ConcurrentDictionary<EntryCacheCategories, CacheContent>();
                    m_oCacheContents.TryAdd(key, d);
                }
                CacheContent c = null;
                if (!d.TryGetValue(categories, out c))
                {
                    c = new CacheContent()
                    {
                        szPlatform = platform,
                        szPaid = paid,
                        szBindKey = bindkey,
                        eEcc = categories,
                        jToken = null,
                        szJson = null,
                        szJsonRepr = null,
                        dtLastTime = DateTime.MinValue,
                        dtTickTime = DateTime.MinValue
                    };
                    d.TryAdd(categories, c);
                }
                return c;
            }
        }

        public static bool TryGetRedisClient(Action<IRedisClient> callback)
        {
            if (callback == null)
            {
                return false;
            }
            try
            {
                using (IRedisClient redis = RedisClientManager.GetDefault()?.GetClient())
                {
                    if (redis == null)
                    {
                        return false;
                    }
                    try
                    {
                        callback(redis);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetEntryKey(string platform, string paid)
        {
            var key = ENTRY_KEY_NAME + "." + platform;
            if (!string.IsNullOrEmpty(paid))
            {
                key += "." + paid;
            }
            return key;
        }

        private static string GetServersDictKey(string platform, string paid)
        {
            var key = SERVER_DICT_KEY_NAME + "." + platform;
            if (!string.IsNullOrEmpty(paid))
            {
                key += "." + paid;
            }
            return key;
        }

        private static string GetServersListKey(string platform, string paid)
        {
            var key = SERVER_LIST_KEY_NAME + "." + platform;
            if (!string.IsNullOrEmpty(paid))
            {
                key += "." + paid;
            }
            return key;
        }

        private class RefreshContent
        {
            public int code;
            public RefreshEntities tag;
        }

        public object Refresh(string platform)
        {
            platform = string.IsNullOrEmpty(platform) ? DEFAULT_PLATFORM : platform;

            IEntryGateway gateway = ServiceObjectContainer.Get<IEntryGateway>();
            if (gateway == null)
            {
                return new RefreshContent { code = 1 };
            }
            RefreshEntities entities = gateway.Refresh(platform);
            if (entities == null)
            {
                return new RefreshContent { code = 2 };
            }
            if (entities.Entities == null)
            {
                return new RefreshContent { code = 3 };
            }
            if (entities.Entities.Count <= 0)
            {
                return new RefreshContent { code = 4 };
            }
            // 缓存到集群系统之中
            TryGetRedisClient((redis) =>
            {
                foreach (var entry in entities.Entities.Values)
                {
                    string keyEntry = GetEntryKey(platform, entry.Paid);
                    string keyServersDict = GetServersDictKey(platform, entry.Paid);
                    string keyServerList = GetServersListKey(platform, entry.Paid);

                    AddCacheContentRef(platform, entry.Paid, EntryCacheCategories.EntryPoint, keyEntry);
                    AddCacheContentRef(platform, entry.Paid, EntryCacheCategories.ServersDictionary, keyServersDict);
                    AddCacheContentRef(platform, entry.Paid, EntryCacheCategories.ServersList, keyServerList);

                    string jsonEntry = XiYouSerializer.SerializableJson(entry.EntryInfo);
                    redis.Set(keyEntry, jsonEntry);

                    string jsonServerDict = XiYouSerializer.SerializableJson(entry.Servers);
                    redis.Set(keyServersDict, jsonServerDict);

                    string jsonServerList = XiYouSerializer.SerializableJson(entry.ServerList);
                    redis.Set(keyServerList, jsonServerList);

                    DateTime dtCurrentTime = DateTime.Now;

                    var cacheContent = GetCacheContentRef(platform, entry.Paid, EntryCacheCategories.EntryPoint);
                    cacheContent.szJson = jsonEntry;
                    cacheContent.szJsonRepr = jsonEntry;
                    cacheContent.jToken = entry.EntryInfo;
                    cacheContent.dtLastTime = dtCurrentTime;
                    cacheContent.dtTickTime = dtCurrentTime;

                    cacheContent = GetCacheContentRef(platform, entry.Paid, EntryCacheCategories.ServersDictionary);
                    cacheContent.szJson = jsonServerDict;
                    cacheContent.jToken = entry.Servers;
                    cacheContent.dtLastTime = dtCurrentTime;
                    cacheContent.dtTickTime = dtCurrentTime;

                    cacheContent = GetCacheContentRef(platform, entry.Paid, EntryCacheCategories.ServersList);
                    cacheContent.szJson = jsonServerList;
                    cacheContent.jToken = entry.ServerList;
                    cacheContent.dtLastTime = dtCurrentTime;
                    cacheContent.dtTickTime = dtCurrentTime;
                }
            });
            return new RefreshContent { code = 0, tag = entities };
        }

        private CacheContent GetCacheContentRef(string platform, string paid, EntryCacheCategories categories)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return null;
            }
            string key = GetCacheContentKeyRef(platform, paid);
            lock (m_oCacheContents)
            {
                ConcurrentDictionary<EntryCacheCategories, CacheContent> d = null;
                m_oCacheContents.TryGetValue(key, out d);
                if (d == null)
                {
                    m_oCacheContents.TryGetValue(platform, out d);
                }
                if (d == null)
                {
                    return null;
                }
                d.TryGetValue(categories, out CacheContent r);
                return r;
            }
        }

        public string GetCacheContentText(string platform, string paid, EntryCacheCategories categories)
        {
            platform = string.IsNullOrEmpty(platform) ? DEFAULT_PLATFORM : platform;

            var content = GetCacheContentRef(platform, paid, categories);
            if (content == null)
            {
                return string.Empty;
            }

            if (content.eEcc != EntryCacheCategories.EntryPoint)
            {
                return content.szJson;
            }

            return content.szJsonRepr;
        }

        public JToken GetCacheContentToken(string platform, string paid, EntryCacheCategories categories)
        {
            platform = string.IsNullOrEmpty(platform) ? DEFAULT_PLATFORM : platform;

            var content = GetCacheContentRef(platform, paid, categories);
            if (content == null)
            {
                return null;
            }

            return content.jToken;
        }

        public virtual ServerAddError Add(ServerAddInfo model)
        {
            if (0 >= model.sid)
            {
                return ServerAddError.ServerAddError_ThePlatformServerIdIsNotAllowedToBeNullOrLessOrEquals0;
            }

            if (0 >= model.aid)
            {
                return ServerAddError.ServerAddError_TheGameAreaServerIdIsNotAllowedToBeNullOrLessOrEquals0;
            }

            if (IPEndPoint.MinPort >= model.gamesvr_port || model.gamesvr_port > IPEndPoint.MaxPort)
            {
                return ServerAddError.ServerAddError_TheGamesvrPortRangeIsLessThan0OrEqualTo65535OrGreater;
            }

            if (IPEndPoint.MinPort >= model.chatsvr_port || model.chatsvr_port > IPEndPoint.MaxPort)
            {
                return ServerAddError.ServerAddError_TheChatsvrPortRangeIsLessThan0OrEqualTo65535OrGreater;
            }

            model.gamesvr_address = (model.gamesvr_address ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(model.gamesvr_address))
            {
                return ServerAddError.ServerAddError_TheGamesvrAddressMayNotBeAnEmptyOrFullyBlankCharacterSet;
            }

            model.chatsvr_address = (model.chatsvr_address ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(model.chatsvr_address))
            {
                return ServerAddError.ServerAddError_TheChatsvrAddressMayNotBeAnEmptyOrFullyBlankCharacterSet;
            }

            model.server_name = (model.server_name ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(model.server_name))
            {
                return ServerAddError.ServerAddError_TheServerNameMayNotBeAnEmptyOrFullyBlankCharacterSet;
            }

            model.paid = (model.paid ?? string.Empty).TrimStart().TrimEnd();
            model.group_name = (model.group_name ?? string.Empty).TrimStart().TrimEnd();

            model.platform = (model.platform ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(model.platform))
            {
                model.platform = DEFAULT_PLATFORM;
            }

            ServerAddError error = ServiceObjectContainer.Get<IEntryGateway>().Add(model);
            if (error != ServerAddError.ServerAddError_Success)
            {
                return error;
            }

            RefreshContent content = Refresh(model.platform) as RefreshContent;
            if (0 != content.code)
            {
                return ServerAddError.ServerAddError_TheEntryPointWasAddedOrModifiedSuccessfullyButCouldNotBeRefreshed;
            }

            return ServerAddError.ServerAddError_Success;
        }
    }
}
