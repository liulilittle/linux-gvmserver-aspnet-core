namespace GVMServer.Web.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    public class ServerConfiguration
    {
        public class Server
        {
            [JsonProperty( PropertyName = "Address" )]
            public string Address { get; set; }

            [JsonProperty( PropertyName = "Port" )]
            public int Port { get; set; }

            [JsonProperty( PropertyName = "Extension" )]
            public string Extension { get; set; }
        }

        [JsonProperty(PropertyName = "ChatServer" )]
        public Server ChatServer { get; set; }

        [JsonProperty( PropertyName = "GameServer" )]
        public Server GameServer { get; set; }

        [JsonProperty( PropertyName = "ServerId" )]
        public string ServerId { get; set; }

        [JsonProperty( PropertyName = "Platform" )]
        public string Platform { get; set; }

        [JsonProperty( PropertyName = "AreaId" )]
        public int AreaId { get; set; }

        [JsonProperty( PropertyName = "UpdateUri" )]
        public string UpdateUri { get; set; }

        [JsonProperty( PropertyName = "SelectToken" )]
        public int? SelectToken { get; set; }

        public static ServerConfiguration[] GetAllConfiguration()
        {
            return Startup.GetDefaultConfiguration().GetSection( "Servers" ).Get<ServerConfiguration[]>();
        }

        public static ServerConfiguration GetConfiguration(string serverId)
        {
            IConfigurationSection section = Startup.GetDefaultConfiguration().GetSection( "Servers" ).GetSection( serverId );
            if ( section == null)
            {
                return null;
            }
            return section.Get<ServerConfiguration>();
        }
    }
}
