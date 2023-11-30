namespace GVMServer.Web.Controllers
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Model.Enum;
    using GVMServer.Web.Service;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class DotController : Controller
    {
        public static bool IsPaidMustEffective()
        {
            return Startup.GetDefaultConfiguration().GetSection("XiYouSdk").GetSection("PaidMustEffective").Get<bool>();
        }

        [HttpGet("Put")]
        public GenericResponse<object, DotPutError> Put() // 打点
        {
            GenericResponse<object, DotPutError> response = new GenericResponse<object, DotPutError>();

            var context = base.HttpContext;
            string userId = context.GetQueryValue("uid");
            int code = 0;
            string s = context.GetQueryValue("code");
            if (s == null)
            {
                response.Code = DotPutError.CodeIsNull;
            }
            else if ((s = s.Trim()).Length <= 0)
            {
                response.Code = DotPutError.CodeIsEmpty;
            }
            else if (!int.TryParse(s, out code))
            {
                response.Code = DotPutError.CodeIsNotNumber;
            }
            else if (userId == null)
            {
                response.Code = DotPutError.UserIdIsNull;
            }
            else if ((userId = userId.Trim()).Length <= 0)
            {
                response.Code = DotPutError.UserIdIsEmpty;
            }
            else
            {
                string paid = context.GetQueryValue("paid");
                if (DotController.IsPaidMustEffective())
                {
                    if (paid == null)
                    {
                        response.Code = DotPutError.PaidIsNull;
                    }
                    else if ((paid = paid.Trim()).Length <= 0)
                    {
                        response.Code = DotPutError.PaidIsEmpty;
                    }
                }
                if (response.Code == DotPutError.Success)
                {
                    response.Code = ServiceObjectContainer.Get<IDotService>().Put(userId, paid, code);
                }
            }
            return response;
        }
    }
}