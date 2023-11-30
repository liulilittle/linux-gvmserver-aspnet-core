namespace GVMServer.W3Xiyou.Controllers
{
    using GVMServer.Log;
    using GVMServer.Net.Web;
    using GVMServer.Net.Web.Mvc.Controller;
    using GVMServer.Serialization.Ssx;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Net;
    using GVMServer.W3Xiyou.Net.Mvh;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Specialized;
    using JsonPropertyAttribute = Newtonsoft.Json.JsonPropertyAttribute;

    /// <summary>
    /// 支付结果通知接口（商品发放通知）
    /// </summary>
    public class PayController : Controller
    {
        private const string AckCompletionOfPaymentOrder_Start = "AckCompletionOfPaymentOrder_Start";
        private const string AckCompletionOfPaymentOrder_None = "AckCompletionOfPaymentOrder_None";
        private const string AckCompletionOfPaymentOrder_Error = "AckCompletionOfPaymentOrder_Error";
        private const string AckCompletionOfPaymentOrder_Request = "AckCompletionOfPaymentOrder_Request";

        private class PayAckOrderRequest
        {
            [JsonProperty("productName")]
            public string ProductName { get; set; }

            [JsonProperty("orderNo")]
            public string OrderNo { get; set; }

            [JsonProperty("channelOrderNo")]
            public string ChannelOrderNo { get; set; }

            [JsonProperty("userID")]
            public string UserID { get; set; }

            [JsonProperty("money")]
            public int Money { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("buyType")]
            [SignIgnore]
            public int BuyType { get; set; }

            [JsonProperty("platform")]
            [SignIgnore]
            public string Platform { get; set; }

            [JsonProperty("appID")]
            public string AppID = null;

            [JsonProperty("sign")]
            [SignIgnore]
            public string Sign = null;
        }

        private enum PaySignError : byte
        {
            kNullOrEmptySignStr = 1,
            kSignStrNotEquals,
            kUnknowError
        }

        public static IConfigurationSection GetConfiguration()
        {
            return MainApplication.GetDefaultConfiguration().GetSection("GMControl");
        }

        private PayAckOrderRequest ParseNotifyObject(NameValueCollection s, out PaySignError signError)
        {
            PaySignError error = 0;
            PayAckOrderRequest info = null;
            try
            {
                if (s != null)
                {
                    info = XiYouSerializer.DeserializeObject<PayAckOrderRequest>(s, (form, signStr) =>
                    {
                        try
                        {
                            if (GetConfiguration().GetValue<bool>("IgnoreSignatureVerification"))
                            {
                                return true;
                            }
                            string inpSign = form.Get("sign") ?? string.Empty; // 输入的已加签名
                            if (string.IsNullOrEmpty(inpSign) || string.IsNullOrEmpty(signStr))
                            {
                                error = PaySignError.kNullOrEmptySignStr;
                            }
                            else
                            {
                                string appID = form.Get("appID");
                                XiYouSdkConfiguration configuration = XiYouSdkConfiguration.GetConfiguration(appID);

                                string rawSign = XiYouUtility.SignText(signStr, configuration); // 原始的已加签名
                                if (inpSign.EqualsAndIgnoreCast(rawSign))
                                {
                                    error = PaySignError.kSignStrNotEquals;
                                    return false;
                                }
                            }
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    });
                }
            }
            catch (Exception)
            {
                error = PaySignError.kUnknowError;
            }
            signError = error;
            return info;
        }

        public static string GetCppFormatterText()
        {
            return CppStaticBinaryFormatter.CreateFormatterText(typeof(PayAckOrderRequest));
        }

        private static void ClearRequestFiles(HttpContext context)
        {
            if (null != context)
            {
                context.Request.Files.Clear();
            }
        }

        private sealed class OrderExtension
        {
            public int Identifier { get; set; }

            public string Platform { get; set; }
        };

        /// <summary>
        /// 对接的通知接口
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        [HttpPost("/api/gm/pay/notify")]
        public void Notify(HttpContext context)
        {
            StatisticsController.GetDefaultController().AddCounter(AckCompletionOfPaymentOrder_Start);
            StatisticsController.GetDefaultController().StartStopWatch(AckCompletionOfPaymentOrder_Request);

            ClearRequestFiles(context);
            PayAckOrderRequest request = ParseNotifyObject(context.Request.Form, out PaySignError signError);
            if (request == null || request.Money < 0 || signError == PaySignError.kUnknowError)
            {
                this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter, "FAILED");
            }
            else if (signError == PaySignError.kNullOrEmptySignStr || signError == PaySignError.kSignStrNotEquals)
            {
                if (signError == PaySignError.kNullOrEmptySignStr)
                {
                    this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kTokenIsNullValue, "SIGN_ERROR");
                }
                else
                {
                    this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kTokenIsNonError, "SIGN_ERROR");
                }
            }
            else
            {
                OrderExtension extension = null;
                if (!string.IsNullOrEmpty(request.Extension))
                {
                    try
                    {
                        JObject jo = XiYouSerializer.DeserializeJson<JObject>(request.Extension);
                        if (jo == null || jo.Count <= 0)
                        {
                            extension = default(OrderExtension);
                        }
                        else if (!jo.ContainsKey("Identifier")) // 兼容模式
                        {
                            extension = new OrderExtension() { Platform = string.Empty, Identifier = 9997 };
                        }
                        else
                        {
                            extension = jo.ToObject<OrderExtension>();
                        }
                    }
                    catch (Exception)
                    {
                        extension = default(OrderExtension);
                    }
                }
                if (extension == null || extension.Identifier <= 0)
                {
                    this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter, "FAILED");
                }
                else
                {
                    this.HandleWebInvoke(context, extension.Platform, extension.Identifier, request);
                }
            }
        }

        private void ResponseWriteError(HttpContext context, XiYouSdkNonError error, string message)
        {
            if (context == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "FAILED";
            }

            GenericResponse<object> response = new GenericResponse<object>();
            response.Tag = Type.Missing;
            response.Code = (int)error;
            response.Message = message;

            context.Response.Write(response.Message/*XiYouSerializer.SerializableObject(response)*/);
            context.Response.End();

            if (error == XiYouSdkNonError.XiYouSdkNonError_kOK)
            {
                StatisticsController.GetDefaultController().AddCounter(AckCompletionOfPaymentOrder_None);
            }
            else
            {
                StatisticsController.GetDefaultController().AddCounter(AckCompletionOfPaymentOrder_Error);
            }
            StatisticsController.GetDefaultController().StopStopWatch(AckCompletionOfPaymentOrder_Request);
        }

        private void HandleWebInvoke(HttpContext context, string platform, long identifier, PayAckOrderRequest message)
        {
            SocketMvhApplication mvh = MainApplication.SocketMvhApplication;
            if (mvh.Invoker.InvokeAsync<GenericResponse<byte>>(platform, identifier, (ushort)XiYouSdkCommands.XiYouSdkCommands_PayAckOrder,
                message,
                (error, model) => this.ResponseWebClient(context, error, model)))
            {
                context.Asynchronous = true; // 异步响应请求，可能长时间挂起
            }
            else
            {
                this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kError, "FAILED");
            }
        }

        private void ResponseWebClient(HttpContext context, SocketReuqestInvokerError error, GenericResponse<byte> response)
        {
            if (error == SocketReuqestInvokerError.Error || response == null)
            {
                this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kError, "FAILED");
            }
            else if (error == SocketReuqestInvokerError.Timeout)
            {
                this.ResponseWriteError(context, XiYouSdkNonError.XiYouSdkNonError_kTimeout, "FAILED");
            }
            else if (error == SocketReuqestInvokerError.Success)
            {
                this.ResponseWriteError(context, unchecked((XiYouSdkNonError)response.Code), response.Message);
            }
        }
    }
}
