namespace GVMServer.Web.Controllers
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using GVMServer.Web.Service;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CdKeyController : Controller
    {
        [HttpGet("exchange")]
        public GenericResponse<CdKeyInfo, CdKeyExchangeError> Exchange()
        {
            GenericResponse<CdKeyInfo, CdKeyExchangeError> response = new GenericResponse<CdKeyInfo, CdKeyExchangeError>();
            do
            {
                HttpContext context = base.HttpContext;
                string cdkey = (context.GetQueryValue("cdkey") ?? string.Empty).Trim().ToLower();
                string userid = (context.GetQueryValue("userid") ?? string.Empty).Trim();
                string platform = (context.GetQueryValue("platform") ?? string.Empty).Trim();

                ulong roleid = 0;
                int areaid = 0;
                if (string.IsNullOrEmpty(cdkey))
                {
                    response.Code = CdKeyExchangeError.CdKeyIsNullOrEmptyString;
                    break;
                }
                else if (string.IsNullOrEmpty(platform))
                {
                    response.Code = CdKeyExchangeError.PlatformIsNullOrEmptyString;
                    break;
                }
                else if (string.IsNullOrEmpty(userid))
                {
                    response.Code = CdKeyExchangeError.UserIdIsNullOrEmptyString;
                    break;
                }
                else
                {
                    string s = context.GetQueryValue("roleid");
                    if (string.IsNullOrEmpty(s))
                    {
                        response.Code = CdKeyExchangeError.RoleIdIsNullOrEmptyString;
                        break;
                    }

                    if (!ulong.TryParse(s, out roleid) || roleid <= uint.MaxValue)
                    {
                        response.Code = CdKeyExchangeError.RoleIdNotIsUint64FormatNumber;
                        break;
                    }

                    s = context.GetQueryValue("areaid");
                    if (string.IsNullOrEmpty(s))
                    {
                        response.Code = CdKeyExchangeError.AreaIdIsNullOrEmptyString;
                        break;
                    }

                    if (!int.TryParse(s, out areaid) || 0 >= areaid || areaid > ushort.MaxValue)
                    {
                        response.Code = CdKeyExchangeError.AreaIdNotIsUint32FormatNumber;
                        break;
                    }
                }

                var result = ServiceObjectContainer.Get<ICdKeyService>().Exchange(cdkey, areaid, userid, roleid, platform);
                response.Code = result.Item1;
                response.Tag = result.Item2;
            } while (false);
            return response;
        }
    }
}