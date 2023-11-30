namespace GVMServer.Web.Model.Enum
{
    public enum AccountLoginError
    {
        Success, // 登录成功（绝对的）
        LoggedButIsUnableToCorrectPutDot, // 登录完成但无法正确打点（相对）
        TokenIsNull, // 令牌是NULL
        TokenIsEmpty, // 令牌是空的
        MacIsNull, // 令牌是NULL
        MacIsEmpty, // 令牌是空的
        PaidIsNull, // PAID是NULL
        PaidIsEmpty, // PAID是空的
        IPAddressIsWrong, // IP地址是错误的
        ProcessIsTimeout, // 处理已超时
        TokenUseTimeout, // 令牌使用超时
        UserVerifiedFailure, // 用户验证失败
        UserNotExists, // 用户不存在
        ChannelNotExists, // 渠道不存在
        NotAccountLoginResponse, // 没有账户登录返回
        UnableToCreateTransaction, // 无法创建事务对象
        AcquireLockFailure, // 需求区域一致CA*失败
        UnableToGetAccountInfo, // NoSql获取用户失败
        UnableToGetAccountId, // 无法获取账户ID
        UnableToExitLocker, // 无法离开锁
        UnableToOpenRedisClient, // 无法打开存储客户端
        UnableToCloseRedisClient, // 无法关闭存储客户端
        CommitedTransactionFailure, // 提交事务失败
        RollbackTransactionFailure, // 回滚事务失败
    }
}
