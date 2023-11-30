namespace GVMServer.W3Xiyou.Net
{
    public enum XiYouSdkCommands : ushort
    {
        XiYouSdkCommands_Heartbeat = 0x7801, // 心跳
        XiYouSdkCommands_Established = 0x7802, // 建立
        XiYouSdkCommands_LoginAccount = 0x7803, // 账户鉴权
        XiYouSdkCommands_FeedbackAccount = 0x7804, // 用户反馈
        XiYouSdkCommands_CreateOrder = 0x7805, // 创建订单
        XiYouSdkCommands_GMWebInstruction = 0x7806, // GM指令
        XiYouSdkCommands_PayAckOrder = 0x7807, // 支付订单
    }
}
