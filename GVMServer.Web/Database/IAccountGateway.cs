namespace GVMServer.Web.Database
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;

    public interface IAccountGateway : IServiceBase
    {
        int GetAccountId(AccountInfo account);
    }
}
