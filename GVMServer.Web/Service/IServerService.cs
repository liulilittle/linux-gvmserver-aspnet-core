namespace GVMServer.Web.Service
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Configuration;
    using GVMServer.Web.Model.Enum;

    public interface IServerService : IServiceBase
    {
        ServerSelectError Select(ServerConfiguration server, string userId);
    }
}
