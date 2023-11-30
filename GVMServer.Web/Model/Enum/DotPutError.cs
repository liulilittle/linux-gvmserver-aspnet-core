namespace GVMServer.Web.Model.Enum
{
    public enum DotPutError
    {
        Success, // 打点成功
        CodeIsNull, // 代码是NULL
        CodeIsEmpty, // 代码是空的
        CodeIsNotNumber, // 代码不是数字
        CodeNotExists, // 代码不存在
        PaidIsNull, // PAID是NULL
        PaidIsEmpty, // PAID是空的
        UserIdIsNull, // 用户ID是NULL
        UserIdIsEmpty, // 用户ID是空的
        UserNotExists, // 用户找不到或未登入
        UserWrongBson, // 用户BSON错误
        NoSqlUnableToAccess, // NoSql无法访问
        NoSqlGetOperatorFailure, // NoSql获取操作失败
        AppIdNotExists, // NoSql获取操作失败
        NotDotResponse, // 没有打点返回
        ProcessIsTimeout, // 处理已超时
        InvalidParameter, // 无效操作
    }
}
