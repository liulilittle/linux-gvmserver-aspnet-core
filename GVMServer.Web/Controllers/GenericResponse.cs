namespace GVMServer.Web.Controllers
{
    using Newtonsoft.Json;

    public class GenericResponse<T, E>
    {
        [JsonProperty( PropertyName = "code" )]
        public E Code { get; set; }

        [JsonProperty( PropertyName = "message" )]
        public string Message { get; set; }

        [JsonProperty( PropertyName = "tag" )]
        public T Tag { get; set; }
    }
}
