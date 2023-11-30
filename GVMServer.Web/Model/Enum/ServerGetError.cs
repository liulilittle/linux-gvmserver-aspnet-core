namespace GVMServer.Web.Model.Enum
{
    public enum ServerGetError
    {
        Success, // 获取成功
        ServerIdIsNull, // 服务器ID是NULL
        ServerIdIsEmpty, // 服务器ID是空的
        ServerIdIsNotExists, // 服务器ID不存在
    }
}
