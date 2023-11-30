namespace GVMServer.W3Xiyou.Handlers
{
    using System;
    using System.Diagnostics;
    using GVMServer.Log;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Docking.Api;
    using GVMServer.W3Xiyou.Docking.Api.Request;
    using GVMServer.W3Xiyou.Docking.Api.Response;
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [SocketHandler(CommandId = (int)XiYouSdkCommands.XiYouSdkCommands_CreateOrder)]
    public class CreateOrderHandler : SocketHandler
    {
        private const string PlatformUserCreateOrder_Start = "PlatformUserCreateOrder_Start";
        private const string PlatformUserCreateOrder_None = "PlatformUserCreateOrder_None";
        private const string PlatformUserCreateOrder_Error = "PlatformUserCreateOrder_Error";
        private const string PlatformUserCreateOrder_Request = "PlatformUserCreateOrder_Request";

        public override void ProcessRequest(SocketContext context)
        {
            CreateOrderAsync(context);
        }

        private bool ResponseClient(SocketContext context, GenericResponse<CreateOrderResponse> response)
        {
            if (context == null || response == null)
            {
                return false;
            }

            if (response.Code == (int)XiYouSdkNonError.XiYouSdkNonError_kOK)
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserCreateOrder_None);
            }
            else
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserCreateOrder_Error);
            }
            StatisticsController.GetDefaultController().StopStopWatch(PlatformUserCreateOrder_Request);

            return context.Response.Write(response);
        }

        protected virtual int CreateOrderAsync(SocketContext context)
        {
            XiYouSdkClient client = context.GetClient() as XiYouSdkClient;
            if (client == null)
            {
                return 0;
            }

            StatisticsController.GetDefaultController().AddCounter(PlatformUserCreateOrder_Start);
            StatisticsController.GetDefaultController().StartStopWatch(PlatformUserCreateOrder_Request);

            CreateOrderRequest request = null;
            try
            {
                request = context.Request.Read<CreateOrderRequest>();
                if (request != null)
                {
                    request.RoleName = request.RoleName.Es8();
                }
            }
            catch (Exception)
            {
                request = default(CreateOrderRequest);
            }

            if (request == null)
            {
                GenericResponse<CreateOrderResponse> response = new GenericResponse<CreateOrderResponse>()
                {
                    Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter,
                    Message = "invalid create order request"
                };
                ResponseClient(context, response);
                return response.Code;
            }

            JObject extension = null;
            try
            {
                if (!string.IsNullOrEmpty(request.Extension))
                {
                    extension = XiYouSerializer.DeserializeJson<JObject>(request.Extension);
                    if (extension != null)
                    {
                        extension["Platform"] = client.Platform;
                        extension["Identifier"] = client.Identifier;
                    }
                }
            }
            catch (Exception)
            {
                extension = default(JObject);
            }

#if DEBUG_XIYOUAPI_BEGININVOKE_TIME
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            DateTime begintime = DateTime.Now;
#endif
            if (extension == null)
            {
                GenericResponse<CreateOrderResponse> response = new GenericResponse<CreateOrderResponse>()
                {
                    Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter,
                    Message = "invalid order extension"
                };
                ResponseClient(context, response);
                return response.Code;
            }

            request.Extension = extension.ToString(Formatting.None);
            XiYouOrder.CreateAsync(client.Identifier, client.Nomenclature, request, (response) =>
            {
                if (response == null)
                {
                    response = new GenericResponse<CreateOrderResponse>();
                    response.Message = "unknown create error";
                    response.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kError;
                }

                ResponseClient(context, response);
#if DEBUG_XIYOUAPI_BEGININVOKE_TIME
                stopwatch.Stop();
                Console.WriteLine($"XiYouOrder::CreateAsync\r\nBeginInvokeTime={begintime.ToString("yyMMdd HHmmss fff")} Code={response.Code}, ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}");
#endif
            });

            return 0;
        }
    }
}
