namespace GVMServer.Web.Model.Enum
{
    public enum AccountGetError
    {
        Success,
        UserNotExists, // 用户不存在或未登入
        UserIdIsNull,
        UserIdIsEmpty,
        UserWrongBson, // 用户BSON错误
        NoSqlUnableToAccess, // NoSql无法访问
        NoSqlGetOperatorFailure, // NoSql获取操作失败
    }
}
