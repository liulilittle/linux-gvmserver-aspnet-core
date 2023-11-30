namespace GVMServer.Web.Service
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using GVMServer.Cache;
    using GVMServer.DDD.Service;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Docking.Api;
    using GVMServer.Web.Database;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Microsoft.Extensions.Configuration;
    using ServiceStack.Redis;

    public class AccountService : IAccountService
    {
        private AccountLoginError ConvertError<T>(GenericResponse<T> e)
        {
            AccountLoginError error = AccountLoginError.Success;
            if (e == null)
            {
                error = AccountLoginError.TokenIsEmpty;
            }
            else
            {
                XiYouSdkNonError code = unchecked((XiYouSdkNonError)e.Code);
                if (code == XiYouSdkNonError.XiYouSdkNonError_kTimeout)
                {
                    error = AccountLoginError.ProcessIsTimeout;
                }
                else if (code == XiYouSdkNonError.XiYouSdkNonError_kTokenUseTimeout ||
                    code == XiYouSdkNonError.XiYouSdkNonError_kTokenIsNonError)
                {
                    error = AccountLoginError.TokenUseTimeout;
                }
                else if (code == XiYouSdkNonError.XiYouSdkNonError_kUserNotExists)
                {
                    error = AccountLoginError.UserNotExists;
                }
                else if (code == XiYouSdkNonError.XiYouSdkNonError_kUserVerifiedFail)
                {
                    error = AccountLoginError.UserVerifiedFailure;
                }
                else if (code == XiYouSdkNonError.XiYouSdkNonError_kChannelNotExists)
                {
                    error = AccountLoginError.ChannelNotExists;
                }
            }
            return error;
        }

        private static string GetInternalAddress(IConfigurationSection configuration, IPAddress address)
        {
            bool ipMustIsIPV4Format = false;
            if (configuration != null)
            {
                try
                {
                    ipMustIsIPV4Format = configuration.GetSection("IPMustIsIPV4Format").Get<bool>();
                }
                catch (Exception) { }
            }
            return ipMustIsIPV4Format ? new IPAddress(XiYouUtility.GetIPV4Address(address)).ToString() : address.ToString();
        }

        private static int GetMaxLoginToPlatformTimeout(IConfigurationSection configuration)
        {
            int timeout = 0; 
            if (configuration != null)
            {
                try
                {
                    timeout = configuration.GetSection("MaxLoginToPlatformTimeout").Get<int>();
                }
                catch (Exception) { }
            }
            if (timeout <= 0)
            {
                timeout = XiYouUtility.DefaultTimeout;
            }
            return timeout;
        }

        private class AccountHotInfo
        {
            public string UserId { get; set; }
        }

        public static string GetHotKey(string token)
        {
            return "account.hot." + token;
        }

        private (AccountLoginError, AccountInfo) LoginToHot(IConfigurationSection configuration, string token, string mac)
        {
            (AccountLoginError, AccountInfo) error = (AccountLoginError.UserNotExists, null); 
            do
            {
                IRedisClient redis = null;
                try
                {
                    redis = RedisClientManager.GetDefault().GetClient();
                }
                catch (Exception)
                {
                    error = (AccountLoginError.UnableToOpenRedisClient, null);
                    break;
                }
                try
                {
                    AccountHotInfo hot = redis.Get<AccountHotInfo>(GetHotKey(token));
                    if (hot == null)
                    {
                        break;
                    }
                    IDisposable locker = null;
                    try
                    {
                        locker = redis.AcquireLock(AccountInfo.GetLockKey(hot.UserId), AccountInfo.GetAcquireLockTimeout());
                    }
                    catch (Exception)
                    {
                        error = (AccountLoginError.AcquireLockFailure, null);
                        break;
                    }
                    try
                    {
                        AccountInfo cachedAccountInfo = null;
                        try
                        {
                            cachedAccountInfo = redis.Get<AccountInfo>(AccountInfo.GetInfoKey(hot.UserId));
                            if (cachedAccountInfo == null)
                            {
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            error = (AccountLoginError.UnableToGetAccountInfo, null);
                            break;
                        }
                        int LoginTokenExpiredTime = configuration.GetSection("LoginTokenExpiredTime").Get<int>();
                        IRedisTransaction transaction = null;
                        try
                        {
                            transaction = redis.CreateTransaction();
                        }
                        catch (Exception)
                        {
                            error = (AccountLoginError.UnableToCreateTransaction, null);
                            break;
                        }
                        try
                        {
                            transaction.QueueCommand((r) => r.Set(AccountLoginTokenInfo.GetTokenKey(cachedAccountInfo.UserId),
                               new AccountLoginTokenInfo()
                               {
                                   Used = 0,
                                   UserId = cachedAccountInfo.UserId,
                                   LoginTime = DateTime.Now,
                                   Mac = mac,
                               }, new TimeSpan(0, 0, LoginTokenExpiredTime)));
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            error = (AccountLoginError.CommitedTransactionFailure, null);
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception)
                            {
                                error = (AccountLoginError.RollbackTransactionFailure, null);
                            }
                            break;
                        }
                        finally
                        {
                            try
                            {
                                transaction.Dispose();
                            }
                            catch (Exception) { }
                        }
                        error = (AccountLoginError.Success, cachedAccountInfo);
                    }
                    catch (Exception)
                    {
                        error = (AccountLoginError.UnableToGetAccountInfo, null);
                        break;
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
                                error = (AccountLoginError.UnableToExitLocker, null);
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
                            error = (AccountLoginError.UnableToCloseRedisClient, null);
                        }
                    }
                }
            } while (false);
            return error;
        }

        private AccountLoginError GetAccountId(IConfigurationSection config, IRedisClient redis, AccountInfo account)
        {
            if (!config.GetValue<bool>("UseDbAccountId"))
            {
                return AccountLoginError.Success;
            }
            try
            {
                AccountInfo cached = redis.Get<AccountInfo>(AccountInfo.GetInfoKey(account.UserId));
                if (cached == null)
                {
                    account.AccountId = ServiceObjectContainer.Get<IAccountGateway>().GetAccountId(account);
                }
                else
                {
                    account.AccountId = cached.AccountId;
                }
                if (account.AccountId == null || account.AccountId == 0)
                {
                    return AccountLoginError.UnableToGetAccountId;
                }
                return AccountLoginError.Success;
            }
            catch (Exception)
            {
                return AccountLoginError.UnableToGetAccountId;
            }
        }

        private (AccountLoginError, AccountInfo) LoginToPlatform(IConfigurationSection configuration, string token, string mac, string iemio, string idfao, IPAddress address, string clientInfo)
        {
            var e = XiYouAccount.Login<AccountInfo>(token, GetInternalAddress(configuration, address), clientInfo, GetMaxLoginToPlatformTimeout(configuration));
            AccountInfo account = null;
            AccountLoginError error = ConvertError(e);
            do
            {
                if (error != AccountLoginError.Success)
                {
                    break;
                }
                account = e.Tag;
                IRedisClient redis = null;
                try
                {
                    redis = RedisClientManager.GetDefault().GetClient();
                }
                catch (Exception)
                {
                    error = AccountLoginError.UnableToOpenRedisClient;
                    break;
                }
                try
                {
                    if (account == null)
                    {
                        error = AccountLoginError.NotAccountLoginResponse;
                        break;
                    }
                    IDisposable locker = null;
                    try
                    {
                        locker = redis.AcquireLock(AccountInfo.GetLockKey(account.UserId), AccountInfo.GetAcquireLockTimeout());
                    }
                    catch (Exception)
                    {
                        error = AccountLoginError.AcquireLockFailure;
                        break;
                    }
                    try
                    {
                        error = this.GetAccountId(configuration, redis, account);
                        if (error != AccountLoginError.Success)
                        {
                            break;
                        }
                        int LoginTokenExpiredTime = configuration.GetSection("LoginTokenExpiredTime").Get<int>();
                        int PlatformTokenExpiredTime = configuration.GetSection("PlatformTokenExpiredTime").Get<int>();
                        IRedisTransaction transaction = null;
                        try
                        {
                            transaction = redis.CreateTransaction();
                        }
                        catch (Exception)
                        {
                            error = AccountLoginError.UnableToCreateTransaction;
                            break;
                        }
                        account.Mac = mac;
                        account.Idfa = idfao;
                        account.Iemi = iemio;
                        try
                        {
                            transaction.QueueCommand((r) =>
                               r.Set(AccountInfo.GetInfoKey(account.UserId), account));
                            transaction.QueueCommand((r) =>
                               r.Set(AccountLoginTokenInfo.GetTokenKey(account.UserId), new AccountLoginTokenInfo()
                               {
                                   Used = 0,
                                   UserId = account.UserId,
                                   LoginTime = DateTime.Now,
                                   Mac = mac,
                               }, new TimeSpan(0, 0, LoginTokenExpiredTime)));

                            if (IsUseHotCachedOptimization(configuration))
                            {
                                transaction.QueueCommand((r) =>
                                   r.Set(GetHotKey(token), new AccountHotInfo()
                                   {
                                       UserId = account.UserId
                                   }, new TimeSpan(0, 0, PlatformTokenExpiredTime)));
                            }

                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            error = AccountLoginError.CommitedTransactionFailure;
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception)
                            {
                                error = AccountLoginError.RollbackTransactionFailure;
                            }
                            error = AccountLoginError.UnableToCreateTransaction;
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
                                error = AccountLoginError.UnableToExitLocker;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    error = AccountLoginError.UnableToGetAccountInfo;
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
                            error = AccountLoginError.UnableToCloseRedisClient;
                        }
                    }
                }
            } while (false);
            if (error != AccountLoginError.Success)
            {
                account = null;
            }
            return (error, account);
        }

        private static bool IsUseHotCachedOptimization(IConfigurationSection configuration)
        {
            if (configuration == null)
            {
                return false;
            }
            return configuration.GetValue<bool>("UseHotCachedOptimization");
        }

        private static bool IsLoginTimeUsePutDotAsync(IConfigurationSection configuration)
        {
            if (configuration == null)
            {
                return false;
            }
            return configuration.GetValue<bool>("LoginTimeUsePutDotAsync");
        }

        private (AccountLoginError, AccountInfo) PutDotToPlatform(IConfigurationSection config, AccountInfo account, string paid) // 打点（
        {
            IDotService service = ServiceObjectContainer.Get<IDotService>();
            if (IsLoginTimeUsePutDotAsync(config)) // cd=330(token switch userinfo)
            {
                service.PutAsync(account, paid, 330, null);
            }
            else
            {
                DotPutError error = service.Put(account, paid, 330);
                if (error != DotPutError.Success)
                {
                    return (AccountLoginError.LoggedButIsUnableToCorrectPutDot, account);
                }
            }
            return (AccountLoginError.Success, account);
        }

        public (AccountLoginError, AccountInfo) Login(string token, string mac, string paid, string iemio, string idfao, IPAddress address, string clientInfo)
        {
            IConfigurationSection config = AccountInfo.GetConfiguration(); // 其实我也没坷垃，美国也不发达~啊！
            (AccountLoginError, AccountInfo)? result = null;
            if (IsUseHotCachedOptimization(config))
            {
                result = LoginToHot(config, token, mac);
                if (result.Value.Item1 == AccountLoginError.Success)
                {
                    return result.Value;
                }
            }
            result = LoginToPlatform(config, token, mac, iemio, idfao, address, clientInfo);
            if (result.HasValue && result.Value.Item1 == AccountLoginError.Success)
            {
                result = PutDotToPlatform(config, result.Value.Item2, paid);
            }
            return result.Value;
        }

        public (AccountGetError, AccountInfo) Get(string userId)
        {
            if (userId == null)
            {
                return (AccountGetError.UserIdIsNull, null);
            }
            if ((userId = userId.Trim()).Length <= 0)
            {
                return (AccountGetError.UserIdIsEmpty, null);
            }
            try
            {
                using (IRedisClient redis = RedisClientManager.GetDefault().GetClient())
                {
                    return this.UnsafeGet(redis, userId);
                }
            }
            catch (Exception)
            {
                return (AccountGetError.NoSqlUnableToAccess, null);
            }
        }

        public (AccountValidateError, AccountInfo) Validate(int selectToken, string token, string userId, int serverAreaId, IPAddress address, string clientInfo)
        {
            AccountValidateError error = AccountValidateError.Success;
            AccountInfo response = null;
            do
            {
                IRedisClient redis = null;
                try
                {
                    redis = RedisClientManager.GetDefault().GetClient();
                }
                catch (Exception)
                {
                    error = AccountValidateError.UnableToOpenRedisClient;
                    break;
                }
                try
                {
                    IDisposable locker = null;
                    try
                    {
                        locker = redis.AcquireLock(AccountInfo.GetLockKey(userId), AccountInfo.GetAcquireLockTimeout());
                    }
                    catch (Exception)
                    {
                        error = AccountValidateError.AcquireLockFailure;
                        break;
                    }
                    try
                    {
                        if ((response = redis.Get<AccountInfo>(AccountInfo.GetInfoKey(userId))) == null)
                        {
                            error = AccountValidateError.UserIsNotExists;
                            break;
                        }

                        AccountLoginTokenInfo LoginToken = redis.Get<AccountLoginTokenInfo>(AccountLoginTokenInfo.GetTokenKey(userId));
                        if (LoginToken == null)
                        {
                            error = AccountValidateError.UserNotSignIn;
                            break;
                        }

                        if (LoginToken.UserId != userId)
                        {
                            error = AccountValidateError.UserNotThisLoginToken;
                            break;
                        }

                        AccountSelectTokenInfo SelectToken = redis.Get<AccountSelectTokenInfo>(AccountSelectTokenInfo.GetTokenKey(userId));
                        if (SelectToken == null)
                        {
                            error = AccountValidateError.SelectTokenIsNotExists;
                            break;
                        }

                        if (SelectToken.Token != selectToken)
                        {
                            error = AccountValidateError.UserNotThisSelectToken;
                            break;
                        }

                        if (AccountInfo.IsValidateServerAreaToken())
                        {
                            if (SelectToken.AreaId != serverAreaId)
                            {
                                error = AccountValidateError.ServerAreaIsNotSelected;
                                break;
                            }
                        }

                        IConfigurationSection configuration = AccountInfo.GetConfiguration();
                        Debug.Assert(configuration != null);

                        int LoginTokenExpiredTime = AccountLoginTokenInfo.GetExpiredTime(configuration);
                        TimeSpan LoginTokenElapsedTime = unchecked(DateTime.Now - LoginToken.LoginTime);
                        if (LoginTokenElapsedTime.TotalSeconds >= LoginTokenExpiredTime)
                        {
                            error = AccountValidateError.LoginTokenIsExpired;
                            redis.Remove(AccountLoginTokenInfo.GetTokenKey(LoginToken.UserId));
                            break;
                        }

                        int LoginTokenReuseTimes = AccountLoginTokenInfo.GetReuseTimes(configuration);
                        if (LoginTokenReuseTimes > 0)
                        {
                            if (LoginToken.Used >= LoginTokenReuseTimes)
                            {
                                error = AccountValidateError.LoginTokenIsRonreusable;
                                break;
                            }
                        }

                        int SelectTokenExpiredTime = AccountSelectTokenInfo.GetExpiredTime(configuration);
                        TimeSpan SelectTokenElapsedTime = unchecked(DateTime.Now - SelectToken.SelectTime);
                        if (SelectTokenElapsedTime.TotalSeconds >= SelectTokenExpiredTime)
                        {
                            error = AccountValidateError.SelectTokenIsExpired;
                            redis.Remove(AccountSelectTokenInfo.GetTokenKey(userId));
                            break;
                        }

                        int SelectTokenReuseTimes = AccountSelectTokenInfo.GetReuseTimes(configuration);
                        if (SelectTokenReuseTimes <= 0)
                        {
                            break;
                        }

                        if (SelectToken.Used >= SelectTokenReuseTimes)
                        {
                            error = AccountValidateError.SelectTokenIsRonreusable;
                            break;
                        }
                        IRedisTransaction transaction = null;
                        try
                        {
                            transaction = redis.CreateTransaction();
                        }
                        catch (Exception)
                        {
                            error = AccountValidateError.UnableToCreateTransaction;
                            break;
                        }
                        try
                        {
                            TimeSpan expiresInTime = new TimeSpan(Convert.ToInt32(Math.Floor(SelectTokenExpiredTime -
                                    SelectTokenElapsedTime.TotalSeconds)) * 10000 * 1000);

                            transaction.QueueCommand((r) => redis.Set(AccountSelectTokenInfo.GetTokenKey(userId),
                                   new AccountSelectTokenInfo()
                                   {
                                       SelectTime = SelectToken.SelectTime,
                                       Used = unchecked(SelectToken.Used + 1),
                                       Token = SelectToken.Token,
                                       UserId = SelectToken.UserId,
                                       AreaId = SelectToken.AreaId,
                                   }, expiresInTime));

                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            error = AccountValidateError.CommitedTransactionFailure;
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception)
                            {
                                error = AccountValidateError.RollbackTransactionFailure;
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
                                error = AccountValidateError.UnableToExitLocker;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    error = AccountValidateError.UnableToCloseRedisClient;
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
                            error = AccountValidateError.UnableToCloseRedisClient;
                        }
                    }
                }
                if (error == AccountValidateError.Success && !AccountInfo.NotSignInNotPlatformValidate)
                {
                    if (response == null)
                    {
                        error = AccountValidateError.UserIsNotExists;
                        break;
                    }
                    var hr = LoginToPlatform(AccountInfo.GetConfiguration(), token, response.Mac, response.Iemi, response.Idfa, address, clientInfo);
                    switch (hr.Item1)
                    {
                        case AccountLoginError.Success:
                            break;
                        case AccountLoginError.LoggedButIsUnableToCorrectPutDot:
                            break;
                        case AccountLoginError.UserVerifiedFailure:
                            error = AccountValidateError.UserNotSignIn;
                            break;
                        case AccountLoginError.TokenUseTimeout:
                            error = AccountValidateError.TokenUseTimeout;
                            break;
                        case AccountLoginError.ChannelNotExists:
                            error = AccountValidateError.ChannelNotExists;
                            break;
                        case AccountLoginError.NotAccountLoginResponse:
                            error = AccountValidateError.NotAccountLoginResponse;
                            break;
                        case AccountLoginError.UserNotExists:
                            error = AccountValidateError.UserIsNotExists;
                            break;
                        default:
                            error = AccountValidateError.UserSecondLoginToPlatformFailure;
                            break;
                    }
                }
            } while (false);
            if (error != AccountValidateError.Success)
            {
                response = null;
            }
            return (error, response);
        }

        public (AccountGetError, AccountInfo) UnsafeGet(IRedisClient redis, string userId)
        {
            try
            {
                if (!redis.ContainsKey(AccountInfo.GetInfoKey(userId)))
                {
                    return (AccountGetError.UserNotExists, null);
                }
                AccountInfo info = redis.Get<AccountInfo>(AccountInfo.GetInfoKey(userId));
                if (info == null)
                {
                    return (AccountGetError.UserWrongBson, null);
                }
                return (AccountGetError.Success, info);
            }
            catch (Exception)
            {
                return (AccountGetError.NoSqlGetOperatorFailure, null);
            }
        }
    }
}
