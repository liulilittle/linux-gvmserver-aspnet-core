namespace GVMServer.Web.Service
{
    using System;
    using System.Text;
    using GVMServer.Cryptography.Final;
    using GVMServer.DDD.Service;
    using GVMServer.Web.Database;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Microsoft.Extensions.Configuration;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using CdKeyType = GVMServer.Web.Model.CdKeyInfo.CdKeyType;

    public class CdKeyService : ICdKeyService
    {
        private const string DATABASE_NAME = "cdkey";

        public class CdKeyExchangeInfo
        {
            public ObjectId _id { get; set; }

            public string Rid { get; set; }

            public string CdKey { get; set; }

            public ulong RoleID { get; set; }

            public int AreaID { get; set; }

            public string Platform { get; set; }

            public string AccountId { get; set; }
        }

        private IMongoCollection<CdKeyExchangeInfo> GetExchangeInfoCollection(IMongoDatabase database)
        {
            try
            {
                string collectionName = $"cdkeyexchangeinfo";
                var collections = database.GetCollection<CdKeyExchangeInfo>(collectionName);
                // collections.Indexes.CreateOne(new CreateIndexModel<CdKeyExchangeInfo>(Builders<CdKeyExchangeInfo>.IndexKeys.Hashed("Rid")));
                return collections;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetExchangeInfoKey(string cdkey, string s)
        {
            return MD5.ToMD5String(cdkey + s, Encoding.UTF8);
        }

        public static int AcquireLockTimeout
        {
            get
            {
                return Startup.GetDefaultConfiguration().
                    GetSection("Business").
                    GetSection("Service").
                    GetSection("CdKey").
                    GetSection("AcquireLockTimeout").Get<int>();
            }
        }

        public static TimeSpan GetAcquireLockTimeout()
        {
            return new TimeSpan(0, 0, AcquireLockTimeout);
        }

        public (CdKeyExchangeError, CdKeyInfo) Exchange(string cdkey, int areaid, string userid, ulong roleid, string platform)
        {
            (CdKeyExchangeError, CdKeyInfo) result = (CdKeyExchangeError.UnableToGetRedisCachedClient, null);
            CdKeyInfo cdkeyinfo = null;
            do
            {
                IMongoDatabase database = ServiceObjectContainer.Get<IMongoGateway>().GetDatabase(DATABASE_NAME);
                if (database == null)
                {
                    return (CdKeyExchangeError.UnableToDatabaseTheReferences, null);
                }

                if (!EntryService.TryGetRedisClient((redis) =>
                {
                    try
                    {
                        try
                        {
                            IMongoCollection<CdKeyInfo> cdkeyinfos = database.GetCollection<CdKeyInfo>("cdkeyinfo");
                            cdkeyinfo = cdkeyinfos.Find(Builders<CdKeyInfo>.Filter.Eq("Rid", cdkey)).FirstOrDefault();
                            if (cdkeyinfo == null)
                            {
                                result = (CdKeyExchangeError.CdKeyCorrespondingBasicInfoNotExists, null);
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            result = (CdKeyExchangeError.UnableToFromDatabaseInQueryCdKeyBaiscInfo, null);
                            return;
                        }

                        DateTime expiredtime = cdkeyinfo.ExpiredTime; // 过期时间限制
                        if (expiredtime != DateTime.MinValue && DateTime.Now > expiredtime)
                        {
                            result = (CdKeyExchangeError.CdKeyUseTimeIsExpired, null);
                            return;
                        }

                        if (0 != cdkeyinfo.AreaID && cdkeyinfo.AreaID != areaid) // 区服使用限制
                        {
                            result = (CdKeyExchangeError.CdKeyNotAllowInputAreaIdUsed, null);
                            return;
                        }

                        if (!string.IsNullOrEmpty(cdkeyinfo.Platform) && cdkeyinfo.Platform != platform) // 渠道平台限制
                        {
                            result = (CdKeyExchangeError.CdKeyNotAllowThisAPlatformUserUsed, null);
                            return;
                        }

                        string exchangeinfokey = string.Empty;
                        if (cdkeyinfo.Type == (int)CdKeyType.AllServerUseItOnlyOnce)
                        {
                            exchangeinfokey = GetExchangeInfoKey(cdkey, "*");
                        }
                        else if (cdkeyinfo.Type == (int)CdKeyType.EveryoneCanUseItOnce)
                        {
                            exchangeinfokey = GetExchangeInfoKey(cdkey, "." + userid);
                        }
                        else if (cdkeyinfo.Type == (int)CdKeyType.EveryoneRoleIdCanUseItOnce)
                        {
                            exchangeinfokey = GetExchangeInfoKey(cdkey, ":" + roleid);
                        }

                        if (string.IsNullOrEmpty(exchangeinfokey))
                        {
                            result = (CdKeyExchangeError.UnableToGenerateExchangeInfoKey, null);
                            return;
                        }

                        do
                        {
                            IMongoCollection<CdKeyExchangeInfo> exchangeinfos = GetExchangeInfoCollection(database);
                            if (exchangeinfos == null)
                            {
                                result = (CdKeyExchangeError.UnableToFetchToExchangeInfoRecordCollection, null);
                                break;
                            }

                            using (IDisposable locker = redis.AcquireLock(exchangeinfokey, GetAcquireLockTimeout()))
                            {
                                CdKeyExchangeInfo exchangeinfo = null;
                                try
                                {
                                    exchangeinfo = exchangeinfos.Find(Builders<CdKeyExchangeInfo>.
                                        Filter.Eq("Rid", exchangeinfokey)).FirstOrDefault();
                                    if (exchangeinfo != null) // 检测是否已经使用过激活码
                                    {
                                        result = (CdKeyExchangeError.CdKeyNotAllowCurrentRoleIdUsage, null);
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    result = (CdKeyExchangeError.UnableToFromDatabaseInQueryCdKeyExchangeInfo, null);
                                    break;
                                }

                                try
                                {
                                    exchangeinfo = new CdKeyExchangeInfo()
                                    {
                                        CdKey = cdkey,
                                        RoleID = roleid,
                                        Rid = exchangeinfokey,
                                        AccountId = userid,
                                        AreaID = areaid,
                                        Platform = platform
                                    };
                                    exchangeinfos.InsertOne(exchangeinfo);
                                }
                                catch (Exception)
                                {
                                    result = (CdKeyExchangeError.UnableToInsertOneExchangeInfoRecordToDatabase, null);
                                    break;
                                }
                            }

                            result = (CdKeyExchangeError.Success, cdkeyinfo);
                        } while (false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                        result = (CdKeyExchangeError.UnableToExchangeInfoRecordTheAcquireLock, null);
                    }
                }
                ))
                {
                    return result;
                }
            } while (false);
            return result;
        }
    }
}
