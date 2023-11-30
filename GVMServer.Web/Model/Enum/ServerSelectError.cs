namespace GVMServer.Web.Model.Enum
{
    public enum ServerSelectError
    {
        Success, // 获取成功
        ServerIdIsNull, // 服务器ID是NULL
        ServerIdIsEmpty, // 服务器ID是空的
        ServerIsNotExists, // 服务器ID不存在
        UserIdIsNull, // 用户ID是NULL
        UserIdIsEmpty, // 用户ID是空的
        UserIsNotExists, // 用户不存在
        AcquireLockFailure, // 需求区域一致CA*失败
        UserWrongBson, // 用户BSON错误
        NoSqlUnableToAccess, // NoSql无法访问
        NoSqlGetUserFailure, // NoSql获取用户失败
        UnableToCreateTransaction, // NoSql事务处理失败
        UnknownGetUserError, // 未知获取用户错误
        NotErrorButNoUser, // 没有错误但没有用户
        LoginTokenIsRonreusable, // 登录令牌不可重用
        LoginTokenIsNotFound, // 找不到用户登录令牌
        UserNotThisLoginToken, // 用户没有此登录令牌
        LoginTokenIsExpired, // 登录令牌已经过期
        UnableToOpenRedisClient, // 无法打开存储客户端
        UnableToCloseRedisClient, // 无法关闭存储客户端
        CommitedTransactionFailure, // 提交事务失败
        RollbackTransactionFailure, // 回滚事务失败
        UnableToExitLocker, // 无法离开锁
    }
}
