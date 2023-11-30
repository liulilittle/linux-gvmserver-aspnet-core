namespace GVMServer.Web.Database
{
    using GVMServer.DDD.Service;
    using MongoDB.Driver;

    public interface IMongoGateway : IServiceBase
    {
        IMongoDatabase GetDatabase(string database);
    }
}
