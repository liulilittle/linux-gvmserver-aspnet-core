namespace GVMServer.Web.Model.Enum
{
    using System.ComponentModel;

    public enum ServerAddError : sbyte
    {
        [Description("Server information was added or modified successfully")]
        ServerAddError_Success, // 服务器信息添加或修改成功

        [Description("The entry point was added or modified successfully but could not be refreshed")]
        ServerAddError_TheEntryPointWasAddedOrModifiedSuccessfullyButCouldNotBeRefreshed, // 成功添加或者修改但却无法刷新入口

        [Description("The platform serverid is not allowed to be null or less or equals 0")]
        ServerAddError_ThePlatformServerIdIsNotAllowedToBeNullOrLessOrEquals0, // 平台服务器ID不允许为空或小于或等于0

        [Description("The game areaserverid is not allowed to be null or less or equals 0")]
        ServerAddError_TheGameAreaServerIdIsNotAllowedToBeNullOrLessOrEquals0, // 游戏区域服务器ID不允许为空或小于或等于0

        [Description("This platform could not be found in the database")]
        ServerAddError_PlatformNotExists, // 无法找到平台

        [Description("Unable to access ExecuteReader operations from the database")]
        ServerAddError_UnableToAccessExecuteReaderOperationsFromTheDatabase, // 无法访问从对数据库进行ExecuteReader操作

        [Description("Try to access the database many times but because the target instance actively refused it")]
        ServerAddError_TryToAccessDatabaseManyTimesButBecauseTheMachineActivelyRefused, // 尝试多次访问数据库，但是被目标实例积极拒绝

        [Description("Try to access the memorycache-middleware many times but because the target instance actively refused it")]
        ServerAddError_TryToAccessMemoryCacheMiddlewareManyTimesButBecauseTheMachineActivelyRefused, // 尝试多次访问内存缓存中间件，但是被目标实例积极拒绝

        [Description("Missing package information for this platform")]
        ServerAddError_MissingPackageInformationForThisPlatform, // 缺省此平台的包信息

        [Description("Missing settings information for this platform")]
        ServerAddError_MissingSettingsInformationForThisPlatform, // 缺省此平台的设定信息

        [Description("Missing switch list information for this platform")]
        ServerAddError_MissingSwitchInformationForThisPlatform, // 缺省此平台的开关信息

        [Description("The gamesvr port range is less than 0 or equal to 65535 or greater")]
        ServerAddError_TheChatsvrPortRangeIsLessThan0OrEqualTo65535OrGreater, // 游戏服务器端口范围小于等于0或者大于65535

        [Description("The chatsvr port range is less than 0 or equal to 65535 or greater")]
        ServerAddError_TheGamesvrPortRangeIsLessThan0OrEqualTo65535OrGreater, // 聊天服务器端口范围小于等于0或者大于65535

        [Description("The server name may not be an empty or fully blank character set")]
        ServerAddError_TheServerNameMayNotBeAnEmptyOrFullyBlankCharacterSet, // 服务器名称不能是空或者全空白的字符集

        [Description("The gamesvrsvr address may not be an empty or fully blank character set")]
        ServerAddError_TheGamesvrAddressMayNotBeAnEmptyOrFullyBlankCharacterSet, // 服务器名称不能是空或者全空白的字符集

        [Description("The chatsvr address may not be an empty or fully blank character set")]
        ServerAddError_TheChatsvrAddressMayNotBeAnEmptyOrFullyBlankCharacterSet, // 服务器名称不能是空或者全空白的字符集

        [Description("No available database node was found")]
        ServerAddError_NoAvailableDatabaseNodeWasFound, // 未找到可用的数据库节点

        [Description("Unable to pull up the database transaction")]
        ServerAddError_UnableToPullUpTheDatabaseTransaction, // 无法拉起数据库事务

        [Description("The database transaction instance could not be release")]
        ServerAddError_TheDatabaseTransactionInstanceCouldNotBeRelease, // 无法释放数据库事务实例

        [Description("The database transaction instance could not be rollback")]
        ServerAddError_TheDatabaseTransactionInstanceCouldNotBeRollback, // 无法回滚数据库事务实例

        [Description("The database transaction instance could not be commit")]
        ServerAddError_TheDatabaseTransactionInstanceCouldNotBeCommit, // 无法提交数据库事务实例
    }

}
