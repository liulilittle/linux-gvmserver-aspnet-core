namespace GVMServer.Web.Model.Enum
{
    public enum CdKeyExchangeError
    {
        Success,
        CdKeyUseTimeIsExpired,                          // cdkey可使用时间已经过期
        CdKeyTypeNotSupport,                            // cdkey类型不被支持
        CdKeyIsNullOrEmptyString,                       // cdkey字符串是一个NULL或者空的字符串
        RoleIdIsNullOrEmptyString,                      // 角色ID是一个NULL或者空的字符串
        RoleIdNotIsUint64FormatNumber,                  // 角色ID不是一个Uin64格式的数值
        AreaIdIsNullOrEmptyString,                      // 区服ID是一个NULL或者空的字符串
        AreaIdNotIsUint32FormatNumber,                  // 区服ID不是一个Uin32格式的数值
        CdKeyCorrespondingBasicInfoNotExists,           // cdkey对应基础信息不存在
        PlatformIsNullOrEmptyString,                    // 平台标识字符串是空或不存在
        UserIdIsNullOrEmptyString,                      // 用户ID字符串是空或不存在
        CdKeyNotAllowInputAreaIdUsed,                   // cdkey不允许输入的区服使用
        CdKeyNotAllowThisAPlatformUserUsed,             // cdkey不允许此平台用户使用
        CdKeyNotAllowCurrentRoleIdUsage,                // cdkey不允许在被当前角色使用
        UnableToDatabaseTheReferences,                  // 无法获取到数据库的引用
        UnableToGenerateExchangeInfoKey,                // 无法生成激活码兑换信息键
        UnableToGetRedisCachedClient,                   // 无法获取到redis客户端
        UnableToFromDatabaseInQueryCdKeyBaiscInfo,      // 无法从数据库中查询cdkey基础信息
        UnableToFromDatabaseInQueryCdKeyExchangeInfo,   // 无法从数据库中查询激活码兑换信息
        UnableToInsertOneExchangeInfoRecordToDatabase,  // 无法插入一条激活码兑换信息记录到数据库
        UnableToExchangeInfoRecordTheAcquireLock,       // 无法获取到激活码兑换信息记录的分布式锁
        UnableToFetchToExchangeInfoRecordCollection,    // 无法提取到激活码兑换信息记录的集合器
    }
}