namespace GVMServer.Web.Model
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Server = GVMServer.W3Xiyou.Docking.Api.Response.LoginAccountResponse.XiYouGameServer;

    public class AccountInfo
    {
        [JsonProperty("accountId")]
        public int? AccountId { get; set; }

        [JsonProperty("appID")]
        public string AppId { get; set; }

        [JsonProperty("userID")]
        public string UserId { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("channelID")]
        public string ChannelId { get; set; }

        [JsonProperty("channelUserID")]
        public string ChannelUserId { get; set; }

        [JsonProperty("defaultServer")]
        public Server DefaultServer { get; set; }

        [JsonProperty("loggedServers")]
        public IList<Server> LoggedServers { get; set; }

        [JsonProperty("mac")]
        public string Mac { get; set; }

        [JsonProperty("idfa")]
        public string Idfa { get; set; }

        [JsonProperty("iemi")]
        public string Iemi { get; set; }

        public static bool NotSignInNotPlatformValidate
        {
            get
            {
                var configuration = Startup.GetDefaultConfiguration();
                if (configuration == null)
                {
                    return true;
                }
                try
                {
                    return configuration.
                         GetSection("Business").
                         GetSection("Service").
                         GetSection("Account").
                         GetSection("NotSignInNotPlatformValidate").Get<bool>();
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public static int AcquireLockTimeout
        {
            get
            {
                int lockTimeout = 0;
                try
                {
                    lockTimeout = Startup.GetDefaultConfiguration().
                        GetSection("Business").
                        GetSection("Service").
                        GetSection("Account").
                        GetSection("AcquireLockTimeout").Get<int>();
                }
                catch (Exception) { }
                if (lockTimeout <= 0)
                {
                    lockTimeout = 10;
                }
                return lockTimeout;
            }
        }

        public static TimeSpan GetAcquireLockTimeout()
        {
            return new TimeSpan(0, 0, AcquireLockTimeout);
        }

        public static string GetInfoKey(string userId)
        {
            return "account.info." + userId;
        }

        public static string GetLockKey(string userId)
        {
            return "account.lock." + userId;
        }

        public static IConfigurationSection GetConfiguration()
        {
            return Startup.GetDefaultConfiguration().
                            GetSection("Business").
                            GetSection("Service").
                            GetSection("Account");
        }

        public static bool IsValidateIgnoreContents()
        {
            try
            {
                IConfigurationSection configuration = GetConfiguration();
                if (configuration == null)
                {
                    return false;
                }
                return configuration.GetValue<bool>("ValidateIgnoreContents");
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsAllowPrintAccountLoginErrorInfo()
        {
            IConfiguration configuration = GetConfiguration();
            if (configuration == null)
            {
                return false;
            }
            try
            {
                return configuration.GetValue<bool>("AllowPrintAccountLoginErrorInfo");
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsValidateServerAreaToken()
        {
            IConfiguration configuration = GetConfiguration();
            if (configuration == null)
            {
                return false;
            }
            try
            {
                return configuration.GetValue<bool>("ValidateServerAreaToken");
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
