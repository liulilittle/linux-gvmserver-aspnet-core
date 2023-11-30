namespace GVMServer.W3Xiyou.Docking.Gm.Request
{
    using Newtonsoft.Json;

    public class SpeakingNotAllowedRequest
    {
        [JsonProperty( PropertyName = "user_id" )]
        public string UserId { get; set; }

        [JsonProperty( PropertyName = "role_id" )]
        public string RoleId { get; set; }

        [JsonProperty( PropertyName = "type" )]
        public int Type { get; set; }

        [JsonProperty( PropertyName = "expire_time" )]
        public long ExpireTime { get; set; }
    }
}
