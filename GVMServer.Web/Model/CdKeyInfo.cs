namespace GVMServer.Web.Model
{
    using System;
    using MongoDB.Bson;
    using Newtonsoft.Json;

    public class CdKeyInfo
    {
        public enum CdKeyType
        {
            AllServerUseItOnlyOnce, // 全区全服只使用一次（断码）
            EveryoneCanUseItOnce, // 每个人可以使用一次
            EveryoneRoleIdCanUseItOnce, // 每个角色ID可使用一次
        }

        [JsonIgnore]
        public ObjectId _id { get; set; }

        [JsonProperty("cdkey")]
        public string Rid { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonIgnore]
        public int AreaID { get; set; }

        [JsonIgnore]
        public DateTime ExpiredTime { get; set; }

        [JsonIgnore]
        public string Platform { get; set; }

        [JsonProperty("rewardid")]
        public long RewardId { get; set; }

        public enum AckPrizeAwardMode : int
        {
            AckPrizeAwardMode_SendToMailBox,
            AckPrizeAwardMode_SendToBackpacking,
        };

        [JsonProperty("mode")]
        public uint Mode { get; set; }

        [JsonProperty("mailid")]
        public long MailId { get; set; }
    }
}
