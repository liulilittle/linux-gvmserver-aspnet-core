namespace GVMServer.Web.Service
{
    using System;
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;

    public interface IDotService : IServiceBase
    {
        DotPutError Put(string userId, string paid, int code);

        DotPutError Put(AccountInfo account, string paid, int code);

        void PutAsync(AccountInfo account, string paid, int code, Action<DotPutError> callback);
    }
}
