namespace GVMServer.W3Xiyou.Handlers
{
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;

    [SocketHandler( CommandId = ( int )XiYouSdkCommands.XiYouSdkCommands_PayAckOrder, AckTimeout = 3000 )]
    public class PayAckOrderHandler : SocketHandler
    {

    }
}
