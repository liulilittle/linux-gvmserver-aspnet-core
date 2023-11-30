namespace GVMServer.Web.Service
{
    using System.Net;
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using ServiceStack.Redis;

    public interface IAccountService : IServiceBase
    {
        (AccountGetError, AccountInfo) UnsafeGet(IRedisClient redis, string userId);

        (AccountGetError, AccountInfo) Get(string userId);

        (AccountLoginError, AccountInfo) Login(string token, string mac, string paid, string iemio, string idfao, IPAddress address, string clientInfo);

        (AccountValidateError, AccountInfo) Validate(int selectToken, string token, string userId, int serverAreaId, IPAddress address, string clientInfo);
    }
}
