namespace GVMServer.W3Xiyou.Docking.Api
{
    using System;
    using System.Net;
    using GVMServer.W3Xiyou.Docking.Api.Request;
    using GVMServer.W3Xiyou.Docking.Api.Response;

    public class XiYouAccount
    {
        private class XiYouSdkLoginAccountRequest
        {
            [SignIgnore]
            public string token { get; set; }

            [SignIgnore]
            public string cip { get; set; }

            [SignIgnore]
            public string clientInfo { get; set; }
        }

        public static void LoginAsync(string token, uint cip, string clientInfo, int timeout, Action<GenericResponse<LoginAccountResponse>> callback)
        {
            if (cip == 0 || cip == uint.MaxValue)
            {
                callback(new GenericResponse<LoginAccountResponse>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter });
            }
            else
            {
                InternalLogin(token, new IPAddress(cip).ToString(), clientInfo, callback, timeout, true);
            }
        }

        private static void InternalLogin<T>(string token, string cip, string clientInfo, Action<GenericResponse<T>> callback, int timeout, bool asynchronous)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (token == null)
            {
                callback(new GenericResponse<T>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kTokenIsNullValue });
                return;
            }
            if (token.Length <= 0)
            {
                callback(new GenericResponse<T>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kTokenIsNonError });
                return;
            }
            if (string.IsNullOrEmpty(cip))
            {
                callback(new GenericResponse<T>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter });
                return;
            }
            string message = XiYouSerializer.FetchSignTextOrMessage(new XiYouSdkLoginAccountRequest()
            {
                token = token,
                cip = cip,
                clientInfo = clientInfo,
            });
            if (timeout <= 0)
            {
                timeout = XiYouUtility.DefaultTimeout;
            }
            if (!asynchronous)
            {
                GenericResponse<T> response = XiYouUtility.PostToUrl<GenericResponse<T>>("http://apisdk.xiyou-a.com/user/verifyAccount", message, timeout);
                if (response == null)
                {
                    response = new GenericResponse<T>() { Code = ~0 };
                }
                callback(response);
            }
            else
            {
                XiYouUtility.PostToUrlAsync<GenericResponse<T>>("http://apisdk.xiyou-a.com/user/verifyAccount", message, (error, response) =>
               {
                   if (error == XiYouUtility.PostAsyncError.kTimeout)
                   {
                       response = new GenericResponse<T>();
                       response.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kTimeout;
                   }
                   callback(response);
               }, timeout);
            }
        }

        public static void LoginAsync<T>(string token, uint cip, string clientInfo, int timeout, Action<GenericResponse<T>> callback)
        {
            if (cip == 0 || cip == uint.MaxValue)
            {
                callback(new GenericResponse<T>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter });
            }
            else
            {
                InternalLogin(token, new IPAddress(cip).ToString(), clientInfo, callback, timeout, true);
            }
        }

        public static GenericResponse<T> Login<T>(string token, string cip, string clientInfo, int timeout)
        {
            GenericResponse<T> response = null;
            InternalLogin<T>(token, cip, clientInfo, (e) => response = e, timeout, false);
            return response;
        }

        private class XiYouSdkFeedbackAccountRequest
        {
            public string appID { get; set; }

            public string userID { get; set; }

            public string roleID { get; set; }

            public string serverID { get; set; }

            public string reportContent { get; set; }

            [SignIgnore]
            public string sign { get; set; }
        }
        /// <summary>
        /// 用户反馈（此接口主要是响应BUG）
        /// </summary>
        public static void FeedbackAsync(long serverId, FeedbackAccountRequest request, Action<GenericResponse<byte>> callback)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            XiYouSdkConfiguration configuration = XiYouSdkConfiguration.GetConfiguration(request.AppCategory);
            if (configuration == null)
            {
                callback(new GenericResponse<byte>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kCategoryTypeNotExists });
            }
            else if (string.IsNullOrEmpty(request.AccountId) ||
                string.IsNullOrEmpty(request.RoleId) ||
                request.ReportContent == null ||
                (request.ReportContent.Length <= 20 && request.ReportContent.Length > 200))
            {
                callback(new GenericResponse<byte>() { Code = (int)XiYouSdkNonError.XiYouSdkNonError_kInvalidParameter });
            }
            else
            {
                XiYouSdkFeedbackAccountRequest o = new XiYouSdkFeedbackAccountRequest();

                o.reportContent = request.ReportContent;
                o.roleID = request.RoleId;
                o.serverID = serverId.ToString();
                o.userID = request.AccountId;
                o.appID = configuration?.AppId;
                o.sign = XiYouSerializer.FetchSignTextOrMessage(o, request.AppCategory, true);

                string messsage = XiYouSerializer.FetchSignTextOrMessage(o);
                XiYouUtility.PostToUrlAsync<GenericResponse<object>>("http://apisdk.xiyou-a.com/report/bug", messsage, (error, response) =>
                {
                    GenericResponse<byte> e = new GenericResponse<byte>()
                    {
                        Tag = 0xff,
                        Code = (int)XiYouSdkNonError.XiYouSdkNonError_kError
                    };
                    if (response != null)
                    {
                        e.Code = response.Code;
                        e.Message = response.Message;
                    }
                    if (error == XiYouUtility.PostAsyncError.kTimeout)
                    {
                        e.Code = (int)XiYouSdkNonError.XiYouSdkNonError_kTimeout;
                    }
                    callback(e);
                }, XiYouUtility.DefaultTimeout);
            }
        }
    }
}
