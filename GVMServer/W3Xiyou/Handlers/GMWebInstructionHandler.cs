namespace GVMServer.W3Xiyou.Handlers
{
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;

    [SocketHandler( CommandId = ( int )XiYouSdkCommands.XiYouSdkCommands_GMWebInstruction, AckTimeout = 1500, RetryAckCount = 3)]
    public class GMWebInstructionHandler : SocketHandler
    {

    }
}
