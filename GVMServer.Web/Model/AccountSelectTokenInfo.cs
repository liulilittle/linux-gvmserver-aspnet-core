namespace GVMServer.Web.Model
{
    using System;
    using Microsoft.Extensions.Configuration;

    public class AccountSelectTokenInfo
    {
        public string UserId { get; set; }

        public int Used { get; set; }

        public int AreaId { get; set; }

        public int Token { get; set; }

        public DateTime SelectTime { get; set; }

        public static int GetExpiredTime(IConfigurationSection config)
        {
            int r = 0;
            if (config != null)
            {
                try
                {
                    r = config.GetSection("SelectTokenExpiredTime").Get<int>();
                }
                catch (Exception) { }
            }
            if (r <= 0)
            {
                r = 30;
            }
            return r;
        }
        public static int GetReuseTimes(IConfigurationSection config)
        {
            int r = 0;
            if (config != null)
            {
                try
                {
                    r = config.GetSection("SelectTokenReuseTimes").Get<int>();
                }
                catch (Exception) { }
            }
            return r;
        }

        public static string GetTokenKey(string token)
        {
            return "account.token.select." + token;
        }
    }
}
