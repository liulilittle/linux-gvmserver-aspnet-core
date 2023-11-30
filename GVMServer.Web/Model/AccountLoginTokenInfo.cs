namespace GVMServer.Web.Model
{
    using System;
    using Microsoft.Extensions.Configuration;

    public class AccountLoginTokenInfo
    {
        public DateTime LoginTime { get; set; }

        public int Used { get; set; }

        public string UserId { get; set; }

        public string Mac { get; set; }

        public static int GetExpiredTime(IConfigurationSection configuration)
        {
            int expiredTime = 0;
            if (configuration != null)
            {
                try
                {
                    expiredTime = configuration.GetSection("LoginTokenExpiredTime").Get<int>();
                }
                catch (Exception) { }
            }
            if (expiredTime <= 0)
            {
                expiredTime = 300;
            }
            return expiredTime;
        }

        public static int GetReuseTimes(IConfigurationSection configuration)
        {
            if (configuration != null)
            {
                try
                {
                    return configuration.GetSection("LoginTokenReuseTimes").Get<int>();
                }
                catch (Exception) { }
            }
            return 0;
        }

        public static string GetTokenKey(string token)
        {
            return "account.token.login." + token;
        }
    }

}
