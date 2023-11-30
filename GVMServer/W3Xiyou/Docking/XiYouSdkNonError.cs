namespace GVMServer.W3Xiyou.Docking
{
    public enum XiYouSdkNonError : int
    {
        XiYouSdkNonError_kOK = 200,
        XiYouSdkNonError_kError = 201,
        XiYouSdkNonError_kCategoryTypeNotExists = 202,
        XiYouSdkNonError_kTimeout = 203,
        XiYouSdkNonError_kGameNotExists = 1400100002, // 游戏不存在，一般是参数错误
        XiYouSdkNonError_kChannelNotExists = 1400100003, // 渠道不存在，一般是参数错误
        XiYouSdkNonError_kUserVerifiedFail = 1400100005, // 用户验证失败，一般出现在sdk服务器去第三方服务器验证失败
        XiYouSdkNonError_kUserNotExists = 1400100006, // 用户不存在，一般是uid参数错误
        XiYouSdkNonError_kTokenIsNullValue = 1400100008, // token为null
        XiYouSdkNonError_kTokenIsNonError = 1400100009, // token错误
        XiYouSdkNonError_kMoneyValueError = 1400100010, // 金额错误
        XiYouSdkNonError_kCreateOrderFail = 1400100011, // 创建订单失败
        XiYouSdkNonError_kDataSignError = 1400100012, // 签名错误
        XiYouSdkNonError_kTokenUseTimeout = 1400100013, // token超时
        XiYouSdkNonError_kInvalidParameter = 1400100014, // 参数错误
        XiYouSdkNonError_kCurrentPacketError = 1400100015, // 当前包无效
        XiYouSdkNonError_kFetchQrCodeHongBaoError = 1400100016, // 获取二维码红包失败
        XiYouSdkNonError_kRoleIsNull = 1400100017, // 用户为NULL
        XiYouSdkNonError_kRepeatOrderError = 1400100022, //  重复订单
    }
}
