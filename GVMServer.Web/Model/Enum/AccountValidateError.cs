namespace GVMServer.Web.Model.Enum
{
    public enum AccountValidateError
    {
        Success,
        LoginTokenIsExpired, // 登录令牌已过期
        LoginTokenIsRonreusable, // 登录令牌已不可重用
        SelectTokenIsNull, // 选择令牌是NULL
        SelectTokenIsEmpty, // 选择令牌是空的
        SelectTokenIsExpired, // 选择令牌已过期
        SelectTokenIsRonreusable, // 选择令牌已不可重用
        SelectTokenIsNotExists, // 选择令牌不存在
        SelectTokenIsNotNumber, // 选择令牌不是数字
        ServerAreaIsNotSelected, // 区域服务器未被选中
        ServerAreaIdIsNull, // 服务器区域ID是NULL
        ServerAreaIdIsEmpty, // 服务器区域ID是空的
        ServerAreaIdIsNotNumber, // 服务器区域ID不是数字
        UserIdIsNull, // 用户ID是NULL
        UserIdIsEmpty, // 用户ID是空的
        UserIsNotExists, // 用户不存在
        IPNotEqualsLoginIP, // IP地址不等登录IP
        UserNotSignIn, // 用户未登入过
        UserNotThisLoginToken, // 用户没有此令牌
        UserNotThisSelectToken, // 用户没有此选择令牌
        IPAddressIsWrong, // 地址是错误的
        IPAddressNotParse, // 地址无法解析
        UnableToCreateTransaction, // 事物处理失败
        AcquireLockFailure, // 需求区域一致CA*失败
        UserSecondLoginToPlatformFailure, // 用户二次登录到平台失败
        UnableToExitLocker, // 无法离开锁
        TokenUseTimeout, // 令牌使用超时
        UserVerifiedFailure, // 用户验证失败
        ChannelNotExists, // 渠道不存在
        NotAccountLoginResponse, // 没有账户登录返回
        SdkTokenIsNull, // 令牌是NULL
        SdkTokenIsEmpty, // 令牌是空的
        UnableToOpenRedisClient, // 无法打开存储客户端
        UnableToCloseRedisClient, // 无法关闭存储客户端
        CommitedTransactionFailure, // 提交事务失败
        RollbackTransactionFailure, // 回滚事务失败
    }
}
