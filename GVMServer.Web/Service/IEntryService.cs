namespace GVMServer.Web.Service
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Newtonsoft.Json.Linq;

    public enum EntryCacheCategories
    {
        ServersList,
        EntryPoint,
        ServersDictionary,
        MaxCategories,
    }

    public interface IEntryService : IServiceBase
    {
        string GetCacheContentText(string platform, string paid, EntryCacheCategories categories);

        JToken GetCacheContentToken(string platform, string paid, EntryCacheCategories categories);

        object Refresh(string platform);

        ServerAddError Add(ServerAddInfo model);
    }
}
