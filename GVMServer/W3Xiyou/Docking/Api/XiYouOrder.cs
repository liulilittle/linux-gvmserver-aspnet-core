namespace GVMServer.W3Xiyou.Docking.Api
{
    using System;
    using System.Net;
    using GVMServer.Linq;
    using GVMServer.W3Xiyou.Docking.Api.Request;
    using GVMServer.W3Xiyou.Docking.Api.Response;
    using Microsoft.Extensions.Configuration;

    public class XiYouOrder
    {
        private class XiYouSdkCreateOrderRequest
        {
            public string uid { get; set; }

            public int productID { get; set; }

            public string productName { get; set; }

            public string productDesc { get; set; }

            public int buyNum { get; set; }

            public int money { get; set; }

            public string roleID { get; set; }

            public string roleName { get; set; }

            public string roleLevel { get; set; }

            public string serverID { get; set; }

            public string serverName { get; set; }

            public string extension { get; set; }

            public string pid { get; set; }

            public string cip { get; set; }

            [SignIgnore]
            public string sign { get; set; }

            [SignIgnore]
            public string clientInfo { get; set; }

            [SignIgnore]
            public string callbackUrl { get; set; }
        };

        private class XiYouSdkCreateOrderResponse
        {
            public object extension = null;
            public string orderNo = null;
            public int flag = 0;
        };

        private static IConfiguration GetPayControlConfiguration()
        {
            return MainApplication.GetDefaultConfiguration().GetSection("PayControl");
        }

        public static void CreateAsync(long serverId, string serverName, CreateOrderRequest request, Action<GenericResponse<CreateOrderResponse>> callback)
        {
            if (request == null ||
                request.RoleName.IsNullOrEmpty() ||
                request.ProduceName.IsNullOrEmpty() ||
                request.ProduceDesc.IsNullOrEmpty() ||
                request.Extension.IsNullOrEmpty() ||
                request.UserId.IsNullOrEmpty() ||
                request.PidClientFlags.IsNullOrEmpty())
            {
                callback(new GenericResponse<CreateOrderResponse>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter });
            }
            else
            {
                XiYouSdkCreateOrderRequest e = new XiYouSdkCreateOrderRequest()
                {
                    uid = request.UserId,
                    productID = request.ProductId,
                    productName = request.ProduceName,
                    productDesc = request.ProduceDesc,
                    buyNum = request.BuyNum,
                    money = request.Money,
                    roleID = request.RoleId.ToString(),
                    roleName = request.RoleName,
                    roleLevel = request.RoleLevel.ToString(),
                    serverID = serverId.ToString(),
                    serverName = serverName,
                    extension = request.Extension,
                    pid = request.PidClientFlags,
                    cip = new IPAddress(request.ClientIP).ToString(),
                    clientInfo = request.ClientInfo,
                    callbackUrl = GetPayControlConfiguration().GetSection("PayNotifyRawUri").Get<string>(),
                };
                e.sign = XiYouSerializer.FetchSignTextOrMessage(e, request.AppCategory, true);
                string message = XiYouSerializer.FetchSignTextOrMessage(e);
                XiYouUtility.PostToUrlAsync<GenericResponse<XiYouSdkCreateOrderResponse>>("http://apisdk.xiyou-a.com/pay/getOrderID", message, (error, responset) =>
                {
                    GenericResponse<CreateOrderResponse> response = new GenericResponse<CreateOrderResponse>();
                    if (responset == null)
                    {
                        response.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kError;
                    }
                    else if (error == XiYouUtility.PostAsyncError.kSuccess)
                    {
                        response.Code = responset.Code;
                        response.Message = responset.Message;
                        if (responset.Tag != null)
                        {
                            XiYouSdkCreateOrderResponse tag = responset.Tag;
                            response.Tag = new CreateOrderResponse()
                            {
                                Extension = tag.extension?.ToString(),
                                Flag = tag.flag,
                                OrderNo = tag.orderNo,
                            };
                        }
                    }
                    if (error == XiYouUtility.PostAsyncError.kTimeout)
                    {
                        response = new GenericResponse<CreateOrderResponse>();
                        response.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kTimeout;
                    }
                    callback(response);
                }, XiYouUtility.DefaultTimeout);
            }
        }
    }
}
