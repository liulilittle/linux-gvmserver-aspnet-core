namespace GVMServer.W3Xiyou.Handlers
{
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Docking.Api;
    using GVMServer.W3Xiyou.Docking.Api.Request;
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;
    using System;
    using System.Diagnostics;

    [SocketHandler(CommandId = (int)XiYouSdkCommands.XiYouSdkCommands_FeedbackAccount)]
    public class FeedbackAccountHandler : SocketHandler
    {
        public override void ProcessRequest(SocketContext context)
        {
            XiYouSdkClient client = context.GetClient() as XiYouSdkClient;
            if (client != null)
            {
                FeedbackAccountRequest request = null;
                try
                {
                    request = context.Request.Read<FeedbackAccountRequest>();
                }
                catch (Exception)
                {
                    request = default(FeedbackAccountRequest);
                }
                if (request == null)
                {
                    context.Response.Write(new GenericResponse<byte>()
                    {
                        Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter,
                    });
                }
                else
                {
#if DEBUG_XIYOUAPI_BEGININVOKE_TIME
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    DateTime begintime = DateTime.Now;
#endif
                    XiYouAccount.FeedbackAsync(client.Identifier, request, (message) =>
                    {
                        if (message == null)
                        {
                            message = new GenericResponse<byte>();
                            message.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kError;
                        }
                        context.Response.Write(message);
#if DEBUG_XIYOUAPI_BEGININVOKE_TIME
                        stopwatch.Stop();
                        Console.WriteLine($"XiYouAccount::FeedbackAsync\r\nBeginInvokeTime={begintime.ToString("yyMMdd HHmmss fff")} Code={message.Code}, ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}");
#endif
                    });
                }
            }
        }
    }
}
