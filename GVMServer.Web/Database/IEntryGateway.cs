namespace GVMServer.Web.Database
{
    using System.Collections.Generic;
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Newtonsoft.Json.Linq;

    public class RefreshEntities
    {
        public string Platform { get; set; }

        public class Entry
        {
            public string Paid { get; set; }

            public JObject EntryInfo { get; set; }

            public JObject Servers { get; set; }

            public JArray ServerList { get; set; }
        }

        public IDictionary<string, Entry> Entities { get; } = new Dictionary<string, Entry>();
    }

    public interface IEntryGateway : IServiceBase
    {
        RefreshEntities Refresh(string platform);

        IList<string> GetAllPlatform();

        IDictionary<string, HashSet<string>> GetAllEntities();

        IList<string> FindAllPaid(string platform);

        ServerAddError Add(ServerAddInfo model);
    }
}
