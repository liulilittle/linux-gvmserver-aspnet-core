namespace GVMServer.W3Xiyou.Docking.Api.Response
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class LoginAccountResponse
    {
        public class XiYouGameServer
        {
            public enum XiYouGameType
            {
                XiYouGameType_kTest = 0,
                XiYouGameType_kOfficial = 1,
            }

            public enum XiYouGameStatus
            {
                XiYouGameStatus_kMaintenance = 1,
                XiYouGameStatus_kStatus = 2,
            }

            [JsonProperty("servername")]
            public string ServerName { get; set; }

            [JsonProperty("iscommend")]
            public byte IsCommend { get; set; }
            /// <summary>
            /// XiYouGameType
            /// </summary>
            [JsonProperty("type")]
            public byte Type { get; set; }

            [JsonProperty("serverid")]
            public string ServerId { get; set; }
            /// <summary>
            /// XiYouGameStatus
            /// </summary>
            [JsonProperty("status")]
            public byte Status { get; set; }

            [JsonProperty("activeTime")]
            public int ActiveTime { get; set; }

            [JsonProperty("loginTime")]
            public int LoginTime { get; set; }

            [JsonProperty("platform")]
            public string Platform { get; set; }
        };

        [JsonProperty("appID")]
        public string AppId; // 这个不发给GAMESVR /*{ get; set; }*/

        [JsonProperty("userID")]
        public string UserId { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("channelID")]
        public string ChannelId { get; set; }

        [JsonProperty("channelUserID")]
        public string ChannelUserId { get; set; }

        [JsonProperty("defaultServer")]
        public XiYouGameServer DefaultServer { get; set; }

        [JsonProperty("loggedServers")]
        public IList<XiYouGameServer> LoggedServers { get; set; }
    }
}
