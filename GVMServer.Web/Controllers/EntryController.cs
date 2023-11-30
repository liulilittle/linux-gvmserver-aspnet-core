namespace GVMServer.Web.Controllers
{
    using GVMServer.DDD.Service;
    using GVMServer.Web.Service;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Mvc;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class EntryController : Controller
    {
        [HttpGet("Get")]
        public object Get()
        {
            var context = base.HttpContext;
            var platform = context.GetQueryValue("platform");
            var paid = context.GetQueryValue("paid");
            string contents = ServiceObjectContainer.Get<IEntryService>().GetCacheContentText(platform, paid,
                EntryCacheCategories.EntryPoint);
            if (string.IsNullOrEmpty(contents))
            {
                return new GenericResponse<object, int>()
                {
                    Code = 1,
                    Tag = new
                    {
                        Paid = paid,
                        Platform = platform
                    }
                };
            }
            return base.Content(contents);
        }

        [HttpGet("Refresh")]
        public object Refresh()
        {
            var context = base.HttpContext;
            string platform = context.GetQueryValue("platform");
            return ServiceObjectContainer.Get<IEntryService>().Refresh(platform);
        }
    }
}