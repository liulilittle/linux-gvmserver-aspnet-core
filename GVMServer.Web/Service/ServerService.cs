namespace GVMServer.Web.Service
{
    using System;
    using System.Diagnostics;
    using GVMServer.Cache;
    using GVMServer.DDD.Service;
    using GVMServer.Web.Configuration;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using GVMServer.Web.Utilities;
    using ServiceStack.Redis;

    public class ServerService : IServerService
    {
        private (ServerSelectError, AccountInfo) GetAccount(IRedisClient redis, string userId)
        {
            var account = ServiceObjectContainer.Get<IAccountService>().UnsafeGet(redis, userId);
            if (account.Item1 == AccountGetError.UserNotExists)
            {
                return (ServerSelectError.UserIsNotExists, null);
            }
            if (account.Item1 == AccountGetError.NoSqlGetOperatorFailure)
            {
                return (ServerSelectError.NoSqlGetUserFailure, null);
            }
            if (account.Item1 == AccountGetError.NoSqlUnableToAccess)
            {
                return (ServerSelectError.NoSqlUnableToAccess, null);
            }
            if (account.Item1 == AccountGetError.UserWrongBson)
            {
                return (ServerSelectError.UserWrongBson, null);
            }
            if (account.Item1 != AccountGetError.Success)
            {
                return (ServerSelectError.UnknownGetUserError, null);
            }
            if (account.Item2 == null)
            {
                return (ServerSelectError.NotErrorButNoUser, null);
            }
            return (ServerSelectError.Success, account.Item2);
        }

        public ServerSelectError Select(ServerConfiguration server, string accountId)
        {
            ServerSelectError error = ServerSelectError.Success;
            IRedisClient redis = null;
            do
            {
                try
                {
                    redis = RedisClientManager.GetDefault().GetClient();
                }
                catch (Exception)
                {
                    error = ServerSelectError.UnableToOpenRedisClient;
                    break;
                }
                try
                {
                    IDisposable locker = null;
                    try
                    {
                        locker = redis.AcquireLock(AccountInfo.GetLockKey(accountId), AccountInfo.GetAcquireLockTimeout());
                    }
                    catch (Exception)
                    {
                        error = ServerSelectError.AcquireLockFailure;
                        break;
                    }
                    try
                    {
                        var account = GetAccount(redis, accountId);
                        if (account.Item1 != ServerSelectError.Success)
                        {
                            return account.Item1;
                        }

                        var configuration = AccountInfo.GetConfiguration();
                        Debug.Assert(configuration != null);

                        AccountLoginTokenInfo loginTokenInfo = redis.Get<AccountLoginTokenInfo>(AccountLoginTokenInfo.GetTokenKey(accountId));
                        if (loginTokenInfo == null)
                        {
                            return ServerSelectError.LoginTokenIsNotFound;
                        }

                        if (loginTokenInfo.UserId != accountId)
                        {
                            return ServerSelectError.UserNotThisLoginToken;
                        }

                        int LoginTokenExpiredTime = AccountLoginTokenInfo.GetExpiredTime(configuration);
                        TimeSpan LoginTokenElapsedTime = unchecked(DateTime.Now - loginTokenInfo.LoginTime);
                        if (LoginTokenElapsedTime.TotalSeconds >= LoginTokenExpiredTime)
                        {
                            return ServerSelectError.LoginTokenIsExpired;
                        }

                        int LoginTokenReuseTimes = AccountLoginTokenInfo.GetReuseTimes(configuration);
                        if (LoginTokenReuseTimes > 0)
                        {
                            if (loginTokenInfo.Used >= (LoginTokenReuseTimes - 1))
                            {
                                return ServerSelectError.LoginTokenIsRonreusable;
                            }
                        }

                        AccountSelectTokenInfo SelectTokenInfo = redis.Get<AccountSelectTokenInfo>(AccountSelectTokenInfo.GetTokenKey(accountId));

                        int SelectTokenExpiredTime = AccountSelectTokenInfo.GetExpiredTime(configuration);
                        bool ReduceLoginTokenReuseTimes = false;
                        do
                        {
                            if (SelectTokenInfo == null)
                            {
                                ReduceLoginTokenReuseTimes = true;
                                break;
                            }

                            TimeSpan SelectTokenElapsedTime = unchecked(DateTime.Now - SelectTokenInfo.SelectTime);
                            if (SelectTokenElapsedTime.TotalSeconds >= SelectTokenExpiredTime)
                            {
                                ReduceLoginTokenReuseTimes = true;
                                break;
                            }

                            int SelectTokenReuseTimes = AccountSelectTokenInfo.GetReuseTimes(configuration);
                            if (SelectTokenReuseTimes > 0)
                            {
                                if (SelectTokenInfo.Used >= SelectTokenReuseTimes)
                                {
                                    ReduceLoginTokenReuseTimes = true;
                                    break;
                                }
                            }

                            if (SelectTokenInfo.AreaId != server.AreaId)
                            {
                                ReduceLoginTokenReuseTimes = true;
                                break;
                            }
                        } while (false);

                        if (!ReduceLoginTokenReuseTimes)
                        {
                            server.SelectToken = SelectTokenInfo.Token;
                            break;
                        }

                        IRedisTransaction transaction = null;
                        try
                        {
                            transaction = redis.CreateTransaction();
                        }
                        catch (Exception)
                        {
                            error = ServerSelectError.UnableToCreateTransaction;
                            break;
                        }
                        try
                        {
                            TimeSpan ExpiresInTime = new TimeSpan(Convert.ToInt32(Math.Floor(LoginTokenExpiredTime - LoginTokenElapsedTime.TotalSeconds)) * 10000 * 1000);
                            if (LoginTokenReuseTimes > 0)
                            {
                                transaction.QueueCommand((r) => r.Set(AccountLoginTokenInfo.GetTokenKey(accountId),
                                     new AccountLoginTokenInfo() // 更新登录令牌
                                     {
                                         LoginTime = loginTokenInfo.LoginTime,
                                         Mac = loginTokenInfo.Mac,
                                         UserId = accountId,
                                         Used = loginTokenInfo.Used + 1,
                                     }, ExpiresInTime));
                            }
                            server.SelectToken = WebUtility.NewGuidHash32();
                            transaction.QueueCommand((r) => r.Set(AccountSelectTokenInfo.GetTokenKey(accountId),
                                new AccountSelectTokenInfo()
                                {
                                    SelectTime = DateTime.Now,
                                    Used = 0,
                                    UserId = accountId,
                                    AreaId = server.AreaId,
                                    Token = server.SelectToken.Value,
                                }, new TimeSpan(0, 0, SelectTokenExpiredTime)));
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            error = ServerSelectError.CommitedTransactionFailure;
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception)
                            {
                                error = ServerSelectError.RollbackTransactionFailure;
                            }
                        }
                        finally
                        {
                            if (transaction != null)
                            {
                                try
                                {
                                    transaction.Dispose();
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        error = ServerSelectError.AcquireLockFailure;
                    }
                    finally
                    {
                        if (locker != null)
                        {
                            try
                            {
                                locker.Dispose();
                            }
                            catch (Exception)
                            {
                                error = ServerSelectError.UnableToExitLocker;
                            }
                        }
                    }
                }
                finally
                {
                    if (redis != null)
                    {
                        try
                        {
                            redis.Dispose();
                        }
                        catch (Exception)
                        {
                            error = ServerSelectError.UnableToCloseRedisClient;
                        }
                    }
                }
            } while (false);
            return error;
        }
    }
}
