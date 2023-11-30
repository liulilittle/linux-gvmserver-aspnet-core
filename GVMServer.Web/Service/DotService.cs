namespace GVMServer.Web.Service
{
    using System;
    using System.Collections.Generic;
    using GVMServer.DDD.Service;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Docking.Api;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Microsoft.Extensions.Configuration;

    public class DotService : IDotService
    {
        private IConfigurationSection GetConfiguration()
        {
            return Startup.GetDefaultConfiguration().GetSection("XiYouSdk").GetSection("DotContrastTable");
        }

        public IDictionary<string, XiYouDot.DotDotContrast> GetAll()
        {
            IConfiguration configuration = GetConfiguration();
            var result = configuration.Get<IDictionary<string, XiYouDot.DotDotContrast>>();
            return result;
        }

        public XiYouDot.DotDotContrast Get(int code)
        {
            IConfiguration configuration = GetConfiguration();
            var contrast = configuration.GetSection(code.ToString()).Get<XiYouDot.DotDotContrast>();
            return contrast;
        }

        public DotPutError Put(string userId, string paid, int code)
        {
            (AccountGetError Error, AccountInfo Account) info = ServiceObjectContainer.Get<IAccountService>().Get(userId);
            if (info.Error == AccountGetError.UserNotExists)
            {
                return DotPutError.UserNotExists;
            }
            if (info.Error == AccountGetError.UserWrongBson)
            {
                return DotPutError.UserWrongBson;
            }
            if (info.Error == AccountGetError.NoSqlUnableToAccess)
            {
                return DotPutError.NoSqlUnableToAccess;
            }
            if (info.Error == AccountGetError.NoSqlGetOperatorFailure)
            {
                return DotPutError.NoSqlGetOperatorFailure;
            }
            return Put(info.Account, paid, code);
        }

        public DotPutError Put(AccountInfo account, string paid, int code)
        {
            DotPutError error = DotPutError.Success;
            InternalPutHandling(account, paid, code, (e) => error = e, false);
            return error;
        }

        private void InternalPutHandling(AccountInfo account, string paid, int code, Action<DotPutError> callback, bool async)
        {
            if (string.IsNullOrEmpty(account.AppId))
            {
                callback?.Invoke(DotPutError.AppIdNotExists);
                return;
            }
            var contrast = Get(code);
            if (contrast == null)
            {
                callback?.Invoke(DotPutError.CodeNotExists);
                return;
            }
            Action<GenericResponse<object>> onError = (response) =>
            {
                if (response == null)
                {
                    callback?.Invoke(DotPutError.NotDotResponse);
                    return;
                }
                XiYouSdkNonError error = unchecked((XiYouSdkNonError)response.Code);
                if (error == XiYouSdkNonError.XiYouSdkNonError_kTimeout)
                {
                    callback?.Invoke(DotPutError.ProcessIsTimeout);
                    return;
                }
                if (error != XiYouSdkNonError.XiYouSdkNonError_kOK)
                {
                    callback?.Invoke(DotPutError.InvalidParameter);
                    return;
                }
                callback?.Invoke(DotPutError.Success);
            };
            if (!async)
            {
                onError(XiYouDot.Dot(account.AppId, paid, contrast.Code, contrast.Action, account.Mac, account.Mac));
            }
            else
            {
                XiYouDot.DotAsync(account.AppId, paid, contrast.Code, contrast.Action, account.Mac, account.Mac, onError, 1);
            }
        }

        public void PutAsync(AccountInfo account, string paid, int code, Action<DotPutError> callback)
        {
            InternalPutHandling(account, paid, code, callback, true);
        }
    }
}
