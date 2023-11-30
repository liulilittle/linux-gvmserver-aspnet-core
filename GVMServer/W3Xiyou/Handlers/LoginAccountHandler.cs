namespace GVMServer.W3Xiyou.Handlers
{
    using System;
    using System.Diagnostics;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Docking.Api;
    using GVMServer.W3Xiyou.Docking.Api.Request;
    using GVMServer.W3Xiyou.Docking.Api.Response;
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;

    [SocketHandler(CommandId = (int)XiYouSdkCommands.XiYouSdkCommands_LoginAccount)]
    public class LoginAccountHandler : SocketHandler
    {
        public override void ProcessRequest(SocketContext context) // 该接口不需要签名
        {
            LoginAccountRequest request = null;
            try
            {
                request = context.Request.Read<LoginAccountRequest>(); 
            }
            catch (Exception)
            {
                request = default(LoginAccountRequest);
            }
            if (request == null)
            {
                context.Response.Write(new GenericResponse<LoginAccountResponse>()
                {
                    Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter
                });
            }
            else
            {
#if DEBUG_XIYOUAPI_BEGININVOKE_TIME
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                DateTime begintime = DateTime.Now;
#endif
                XiYouAccount.LoginAsync(request?.UserToken, request?.ClientIP ?? 0, request.ClientInfo, XiYouUtility.DefaultTimeout, (message) =>
                {
                    if (message == null)
                    {
                        message = new GenericResponse<LoginAccountResponse>();
                        message.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kError;
                    }
                    context.Response.Write(message);
#if DEBUG_XIYOUAPI_BEGININVOKE_TIME
                    stopwatch.Stop();
                    Console.WriteLine($"XiYouAccount::LoginAsync\r\nBeginInvokeTime={begintime.ToString("yyMMdd HHmmss fff")} Code={message.Code}, ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}");
#endif
                });
            }
        }
    }
}
