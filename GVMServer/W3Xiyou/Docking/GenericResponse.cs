namespace GVMServer.W3Xiyou.Docking
{
    using Newtonsoft.Json;
    using System;

    [Serializable]
    public class GenericResponse<TCode, TTag>
    {
        /// <summary>
        /// XiYouSdkNonError
        /// </summary>
        [JsonProperty("status")]
        public TCode Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public TTag Tag { get; set; }
    }

    [Serializable]
    public class GenericResponse<T> : GenericResponse<int, T>
    {

    }
}
