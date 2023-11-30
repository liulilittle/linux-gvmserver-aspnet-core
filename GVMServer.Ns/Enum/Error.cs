namespace GVMServer.Ns.Enum
{
    public enum Error : ushort
    {
        Error_Success,
        Error_UnknowTheDatabaseExecuteException,                                                                    // 未知的数据库执行异常
        Error_UnableToDeserializeRequestParameters,                                                                 // 无法反序列化请求参数
        Error_UndefinedInputTheApplicationTypeEnum,                                                                 // 没有定义输入的应用类型枚举
        Error_AddressMaskCannotBeASetOfEmptyOrFullWhiteSpaceCharacters,                                             // 地址代码不能是空或者全空白字符集合
        Error_GameServerApplicationNotAllowInputServerNoLessOrEqualsZero,                                           // 游戏服务器应用不允许输入服务器编号小于或等于0
        Error_TheAvailableDataAdapterCouldNotBeFound,                                                               // 找不到可用的数据库适配器
        Error_UnableToGetDatabaseConnection,                                                                        // 无法获取数据库连接
        Error_UnableToOpenDatabaseConnection,                                                                       // 无法打开数据库连接
        Error_TheQueryOperationCannotBePerformedAgainstTheDatabase,                                                 // 无法对数据库执行查询操作
        Error_AttemptsToGenerateMoreThanOneNodeidWereInvalid,                                                       // 尝试多次生成节点Id都失败告终
        Error_NsInstanceModelIsNullReferences,                                                                      // NS实例模块是空引用
        Error_UnableToCreateDatabaseCommand,                                                                        // 无法创建数据库命令
        Error_AddToDatabaseAfterNonQueryTheValueIsLessOrEqualsThanZero,                                             // 添加到数据库之后NonQuery的值小于或等于0
        Error_TheIdProvidingTheAuthenticationIsInvalid,                                                             // 提供鉴权的Id是无效的（一般泛指客户端未进行鉴权身份信息注册）
        Error_TheApplicationTypeIsNotTheSameAsTheRegistrationType,                                                  // 应用类型与注册的类型不一致（一般泛指客户端使用错误的类型进行鉴权）
        Error_TheRedisClientCouldNotBeRetrievedFromTheConnectionPoolManager,                                        // 无法从连接池管理器中获取到RedisClient
        Error_UnableToSetRedisCacheToMemoryServer,                                                                  // 无法设置Redis缓存键到内存服务器
        Error_GetRedisCacheToLocalCacheTimeThrowException,                                                          // 获取Redis缓存键到本地缓存时抛出了异常
        Error_TheDistributedCriticalSectionBlockLockCannotBeObtained,                                               // 无法获取分布式临界区锁
        Error_ProblemsOccurredInReleasingTheDistributedCriticalSectionBlockLocks,                                   // 释放分布式临界区锁的过程中出现了问题
        Error_UnableToConvertDateTableOrDataSetToAllTTypeModelList,                                                 // 无法转换数据表或者数据集到全部T类型模型列表
        Error_UnknowTheUnhandlingExceptionWarning,                                                                  // 未知的未处理异常警告
        Error_NodeidAreNotAllowedToBeEmptyOrFullBlankCharacterSet,                                                  // 节点Id不允许为空或者全空白字符集
        Error_TheSuppliedValueIsNotAValidGuidFormatStringSupportForExampleN_D_B_P_XGuidFormat,                      // 提供的值不是有效的Guid格式字符串，例如支持N_D_B_P_X Guid格式字符串
        Error_SuccessButTheContentAssociatedWithThisGuidCouldNotBeFound,                                            // 成功但是找不到与此Guid关联的内容
        Error_TheAuthenticationRequestDataModelProvidedIsAnEmptyReferences,                                         // 提供的鉴权请求数据模型是空引用
        Error_UnableToAddTheItemToSetTheDistributedCache,                                                           // 无法添加项到分布式缓存集合
        Error_TheTransactionThatCommittedTheRedisCachedDataFailedOnRollback,                                        // 提交Redis缓存数据的事务失败正在回滚
        Error_TheCommitToTheRedisCacheFailedButThereWasAProblemWithTheRollback,                                     // 提交到Redis缓存失败但是回滚过程中遇到了问题
        Error_TheTransactionInstanceOfRedisCannotBeOpened,                                                          // 无法打开Redis的事务实例
        Error_ThePipelineInstanceOfRedisCannotBeOpened,                                                             // 无法打开Redis的管道实例
        Error_TheFlushPipelineToTheRedisCacheFailedButThereWasAProblem,                                             // 刷入到Redis缓存到管道失败
        Error_TheQueueCommandLineForATransactionCannotBeAddedToRedis,                                               // 无法往Redis添加事务的队列命令行
        Error_AKeyValuePairCannotBeRemovedFromTheRedisCacheBuffer,                                                  // 无法在Redis缓存之中删除某个键值对
        Error_UnableToRemoveItemFromSetTheDistributedCache,                                                         // 无法删除项到分布式缓存集合
        Error_UnableToGetAllItemsFromSetTheDistributedCache,                                                        // 无法从分布式缓存集合获取全部项
        Error_MultipleKeyValuesCannotBeRetrievedFromTheRedisCache,                                                  // 无法从分布式缓存之中获取到多个键值
        Error_ApplicationTypeAreNotAllowedToBeEmptyOrFullBlankCharacterSet,                                         // 应用类型不允许为空或者全空白字符集
        Error_UnableToConvertStringToNumber,                                                                        // 无法转换字符串到数值
        Error_UnableToDeserializeResponseJsonContent,                                                               // 无法反序列化响应JSON内容
        Error_ThereWasAFailureInTheAccessApiInterfaceAndItCouldBeATimeoutOrAProblemWithTheSerializedResponse,       // 访问API接口出现故障可能是超时或序列化响应出现问题
        Error_YourInputTheLinkHeartbeatIsNullReferences,                                                            // 你输入的链接心跳是空引用
        Error_YourInputRedisInstanceIsNullReferences,                                                               // 你输入的Redis实例是空引用
        Error_YourInputAkeyCannotBeAnEmptyString,                                                                   // 你输入的Key不可以是空字符串
        Error_SeriousInternalUnhandledExceptionProblems,                                                            // 严重的内部未处理异常
        Error_SeriousCodingErrorsHandingMustBeStrictlyValidAndDoNotAllowTheUseOfAnyNullForm,                        // 严重代码错误Handling必须严格保证不是任何形式的空引用(例：空引用、空流程)
        Error_UnableToDisposingRedisClientObject,                                                                   // 无法释放Redis客户端对象
        Error_UnableToAcquireLockNowIsWaitingTimeoutException,                                                      // 无法获取锁现在已经等待超时
        Error_SuccessSetToRedisCacheMemoryServerButIsReturnFailtrue,                                                // 成功设置到Redis内存服务器但是返回了失败
        Error_AnUncomprehendingErrorThatGoesBeyondTheScopeOfAGivenDesignAndFailsToObtainAReferenceToTheRedisClient, // 一个无法理解的错误它超乎程序的既定设计范围获取RedisClient的引用为空指针
        Error_TheRankingIndexMustNotBeLessThanZero,                                                                 // 排名索引不可以小于0
        Error_ThereWasAnUnexpectedProblemGettingRankingMemberForASliceFromTheShardingStore,                         // 从分片存储器之中获取某个分片之中的排名成员时出现了问题
        Error_AnUnknownRedisExceptionOccurredWhileObtainingTheNumberOfMembersOfTheZSetCollection,                   // 在获取ZSet集合成员数量的过程中发生了一个未知的Redis异常
        Error_KeyNotAllowIsANullOrEmptyString,                                                                      // Key不允许是一个NULL或者空字符串
        Error_YourInputTheRankingMemberIsNullReferences,                                                            // 你输入的排名成员是空引用
        Error_UnableToGetLastRankingMemberElement,                                                                  // 无法获取最后一个排名成员元素
        Error_UnableToExecuteGetItemIndexInSortedSet,                                                               // 无法执行获取排序集合的成员索引
        Error_SuccessRemoveToRedisCacheMemoryServerButIsReturnFailtrue,                                             // 成功从到Redis内存服务器中删除但是返回了失败
        Error_UnableToRemoveInRedisCacheToMemoryServer,                                                             // 无法从Redis缓存键到内存服务器之中删除
        Error_UnableToRequestZrevrangebylexOrZrangebylexCommandFromTheCacheServer,                                  // 无法向缓存服务器请求ZREVRANGEBYLEX or ZRANGEBYLEX命令
        Error_ANullRedisTextIsObtainedAtTheEndOfTheRequestToTheRemoteCacheServer,                                   // 在对远程缓存服务器的请求结束时获得一个空RedisText
        Error_CannotFindLeaderboardObjectsThatMatchTheProvidedsCriteria,                                            // 找不到符合所提供条件的排行榜对象
        Error_TheRequestModelsObjectCouldNotBeReadIn,                                                               // 无法读入请求模型对象
        Error_PlatformTextIsNullOrEmptyThisNotAllow,                                                                // 平台字符串文本是空或者空字符串
        Error_UnableToFetchATheLeaderboardObjectInstance,                                                           // 无法获取一个排行榜对象实例
        Error_UnableToFetchServerLeaderboardTheObjectInstacne,                                                      // 无法获取服务器排行榜对象实例
        Error_RuntimeError,                                                                                         // 运行时错误
        Error_ThisEventIsNotAllowedToOpenWhenTheBattleServerCannotBeMatched,                                        // 无法匹配战斗服务器时这个活动则不被允许向外开放
        Error_ThereAreCurrentlyNoCombatClothingNodesThatAreOnlineInTheCluster,                                      // 当前没有任何处于集群联机的战斗服节点
        Error_MonitorObjectIsRemotingNodeClusterLocking,                                                            // 监视对象远程集群节点锁定中
        Error_UnableToMonitorIsExitNotAnyTheFetchLockerObject,                                                      // 无法离开监视因为没有获取到任何的锁对象
        Error_TheCurrentMonitorObjectHasBeenAcquiredSoThatTheLockerCannotBeReentered,                               // 当前监视对象已获取锁对象不能重入临界
    }
}
