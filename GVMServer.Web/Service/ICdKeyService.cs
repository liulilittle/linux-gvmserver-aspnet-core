namespace GVMServer.Web.Service
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;

    public interface ICdKeyService : IServiceBase
    {
        (CdKeyExchangeError, CdKeyInfo) Exchange(string cdkey, int areaid, string userid, ulong roleid, string platform);
    }
}
