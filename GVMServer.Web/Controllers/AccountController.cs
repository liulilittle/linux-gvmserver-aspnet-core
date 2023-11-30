namespace GVMServer.Web.Controllers
{
    using System.Net;
    using GVMServer.DDD.Service;
    using GVMServer.Log;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using GVMServer.Web.Service;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private const string PlatformUserAccountLogin_Start = "PlatformUserAccountLogin_Start";
        private const string PlatformUserAccountLogin_None = "PlatformUserAccountLogin_None";
        private const string PlatformUserAccountLogin_Error = "PlatformUserAccountLogin_Error";
        private const string PlatformUserAccountLogin_Request = "PlatformUserAccountLogin_Request";

        private const string PlatformUserAccountValidate_Start = "PlatformUserAccountValidate_Start";
        private const string PlatformUserAccountValidate_None = "PlatformUserAccountValidate_None";
        private const string PlatformUserAccountValidate_Error = "PlatformUserAccountValidate_Error";
        private const string PlatformUserAccountValidate_Request = "PlatformUserAccountValidate_Request";

        [HttpGet("Login")]
        public GenericResponse<AccountInfo, AccountLoginError> Login()
        {
            StatisticsController.GetDefaultController().AddCounter(PlatformUserAccountLogin_Start);
            StatisticsController.GetDefaultController().StartStopWatch(PlatformUserAccountLogin_Request);

            GenericResponse<AccountInfo, AccountLoginError> response = InternalUserLogin();
            if (response.Code == AccountLoginError.Success ||
                response.Code == AccountLoginError.LoggedButIsUnableToCorrectPutDot)
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserAccountLogin_None);
            }
            else
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserAccountLogin_Error);
                if (AccountInfo.IsAllowPrintAccountLoginErrorInfo())
                {
                    var log = LogController.GetDefaultController();
                    if (log != null)
                    {
                        var context = this.HttpContext;
                        log.WarningLine($"{typeof(AccountLoginError).Name}.{response.Code} {context.Request.Protocol} {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
                    }
                }
            }

            StatisticsController.GetDefaultController().StopStopWatch(PlatformUserAccountLogin_Request);
            return response;
        }

        [HttpGet("Validate")]
        public GenericResponse<AccountInfo, AccountValidateError> Validate()
        {
            StatisticsController.GetDefaultController().AddCounter(PlatformUserAccountValidate_Start);
            StatisticsController.GetDefaultController().StartStopWatch(PlatformUserAccountValidate_Request);

            GenericResponse<AccountInfo, AccountValidateError> response = InternalUserValidate();
            if (response.Code == AccountValidateError.Success)
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserAccountValidate_None);
            }
            else
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserAccountValidate_Error);
            }

            StatisticsController.GetDefaultController().StopStopWatch(PlatformUserAccountValidate_Request);
            return response;
        }

        private GenericResponse<AccountInfo, AccountLoginError> InternalUserLogin()
        {
            GenericResponse<AccountInfo, AccountLoginError> response =
                new GenericResponse<AccountInfo, AccountLoginError>();

            HttpContext context = base.HttpContext;
            string token = context.GetQueryValue("token");
            string mac = context.GetQueryValue("mac");
            if (token == null)
            {
                response.Code = AccountLoginError.TokenIsNull;
            }
            else if ((token = token.Trim()).Length <= 0)
            {
                response.Code = AccountLoginError.TokenIsEmpty;
            }
            else if (mac == null)
            {
                response.Code = AccountLoginError.MacIsNull;
            }
            else if ((mac = mac.Trim()).Length <= 0)
            {
                response.Code = AccountLoginError.MacIsEmpty;
            }
            else
            {
                IPAddress address = context.GetRemoteIpAddress();
                if (address == IPAddress.Any)
                {
                    response.Code = AccountLoginError.IPAddressIsWrong;
                }
                else
                {
                    string paid = context.GetQueryValue("paid");
                    if (DotController.IsPaidMustEffective())
                    {
                        if (paid == null)
                        {
                            response.Code = AccountLoginError.PaidIsNull;
                        }
                        else if ((paid = paid.Trim()).Length <= 0)
                        {
                            response.Code = AccountLoginError.PaidIsEmpty;
                        }
                    }
                    string iemio = context.GetQueryValue("iemio");
                    string idfao = context.GetQueryValue("idfao");
                    string clientInfo = context.GetQueryValue("clientInfo");
                    if (string.IsNullOrEmpty(iemio))
                    {
                        iemio = context.GetQueryValue("iemi");
                    }
                    if (string.IsNullOrEmpty(idfao))
                    {
                        idfao = context.GetQueryValue("idfa");
                    }
                    if (response.Code == AccountLoginError.Success)
                    {
                        var e = ServiceObjectContainer.Get<IAccountService>().Login(token, mac, paid, iemio, idfao, address, clientInfo);
                        var account = e.Item2;
                        if (account != null)
                        {
                            account.ChannelId = null;
                            account.AppId = null;
                            account.Mac = null;
                            account.Idfa = null;
                            account.Iemi = null;
                            account.ChannelUserId = null;
                        }
                        response.Code = e.Item1;
                        response.Tag = account;
                    }
                }
            }
            return response;
        }

        private GenericResponse<AccountInfo, AccountValidateError> InternalUserValidate()
        {
            GenericResponse<AccountInfo, AccountValidateError> response =
                new GenericResponse<AccountInfo, AccountValidateError>();

            HttpContext context = base.HttpContext;
            string accountId = context.GetQueryValue("uid");
            if (string.IsNullOrEmpty(accountId))
            {
                accountId = context.GetQueryValue("userid");
            }

            string sdkToken = context.GetQueryValue("sdkToken");
            string selectsToken = context.GetQueryValue("token");
            string clientInfo = context.GetQueryValue("clientInfo");
            do
            {
                if (!AccountInfo.NotSignInNotPlatformValidate)
                {
                    if (sdkToken == null)
                    {
                        response.Code = AccountValidateError.SdkTokenIsNull;
                        break;
                    }

                    if ((sdkToken = sdkToken.Trim()).Length <= 0)
                    {
                        response.Code = AccountValidateError.SdkTokenIsEmpty;
                        break;
                    }
                }

                if (accountId == null)
                {
                    response.Code = AccountValidateError.UserIdIsNull;
                    break;
                }
                if ((accountId = accountId.Trim()).Length <= 0)
                {
                    response.Code = AccountValidateError.UserIdIsEmpty;
                    break;
                }

                if (selectsToken == null)
                {
                    response.Code = AccountValidateError.SelectTokenIsNull;
                    break;
                }

                if ((selectsToken = selectsToken.Trim()).Length <= 0)
                {
                    response.Code = AccountValidateError.SelectTokenIsEmpty;
                    break;
                }

                if (!int.TryParse(selectsToken, out int selectToken))
                {
                    response.Code = AccountValidateError.SelectTokenIsNotNumber;
                    break;
                }

                IPAddress address = context.GetRemoteIpAddress();
                if (address == IPAddress.Any)
                {
                    response.Code = AccountValidateError.IPAddressIsWrong;
                    break;
                }

                int serverAreaId = 0;
                if (AccountInfo.IsValidateServerAreaToken())
                {
                    string s = context.GetQueryValue("said");
                    if (string.IsNullOrEmpty(s))
                    {
                        s = context.GetQueryValue("serverareaid");
                    }
                    if (s == null)
                    {
                        response.Code = AccountValidateError.ServerAreaIdIsNull;
                    }
                    else if ((s = s.Trim()).Length <= 0)
                    {
                        response.Code = AccountValidateError.ServerAreaIdIsEmpty;
                    }
                    else if (!int.TryParse(s, out serverAreaId))
                    {
                        response.Code = AccountValidateError.ServerAreaIdIsNotNumber;
                    }
                }

                if (response.Code == AccountValidateError.Success)
                {
                    var e = ServiceObjectContainer.Get<IAccountService>().Validate(selectToken, sdkToken, accountId, serverAreaId, address, clientInfo);
                    response.Code = e.Item1;
                    response.Tag = e.Item2;
                }
            } while (false);
            if (AccountInfo.IsValidateIgnoreContents())
            {
                response.Tag = null;
            }
            return response;
        }
    }
}