namespace GVMServer.Web.Controllers
{
    using System;
    using GVMServer.DDD.Service;
    using GVMServer.Log;
    using GVMServer.Web.Configuration;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using GVMServer.Web.Service;
    using GVMServer.Web.Utilities;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ServerController : Controller
    {
        private const string PlatformUserSelectServer_Start = "PlatformUserSelectServer_Start";
        private const string PlatformUserSelectServer_None = "PlatformUserSelectServer_None";
        private const string PlatformUserSelectServer_Error = "PlatformUserSelectServer_Error";
        private const string PlatformUserSelectServer_Request = "PlatformUserSelectServer_Request";

        public static JToken GetServer(string platform, string paid, string serverid)
        {
            JObject servers = ServiceObjectContainer.Get<IEntryService>().GetCacheContentToken(platform, paid,
                EntryCacheCategories.ServersDictionary) as JObject;
            if (servers == null)
            {
                return null;
            }
            else
            {
                JToken token = null;
                servers.TryGetValue(serverid, out token);
                return token;
            }
        }

        private GenericResponse<object, ServerGetError> InternalGet(bool serializer)
        {
            GenericResponse<object, ServerGetError> response = new GenericResponse<object, ServerGetError>();

            var context = base.HttpContext;
            string serverid = context.GetQueryValue("sid");
            string paid = context.GetQueryValue("paid");
            string platform = context.GetQueryValue("platform");
            if (serverid == null)
            {
                response.Code = ServerGetError.ServerIdIsNull;
            }
            else if ((serverid = serverid.Trim()).Length <= 0)
            {
                response.Code = ServerGetError.ServerIdIsEmpty;
            }
            else
            {
                JToken configuration = GetServer(platform, paid, serverid);
                if (configuration == null)
                {
                    response.Code = ServerGetError.ServerIdIsEmpty;
                }
                else
                {
                    response.Code = ServerGetError.Success;
                    response.Tag = serializer ? (object)configuration.ToObject<ServerConfiguration>() : configuration;
                }
            }
            return response;
        }

        private GenericResponse<ServerConfiguration, ServerSelectError> InternalSelectServer()
        {
            GenericResponse<ServerConfiguration, ServerSelectError> response = Convert(InternalGet(true));
            if (response.Code != ServerSelectError.Success)
            {
                return response;
            }
            string userId = base.HttpContext.GetQueryValue("uid");
            if (userId == null)
            {
                response.Code = ServerSelectError.UserIdIsNull;
            }
            else if ((userId = userId.Trim()).Length <= 0)
            {
                response.Code = ServerSelectError.UserIdIsEmpty;
            }
            else
            {
                ServerConfiguration server = response.Tag;
                response.Code = ServiceObjectContainer.Get<IServerService>().Select(server, userId);
            }
            if (response.Code != ServerSelectError.Success)
            {
                response.Tag = null;
            }
            return response;
        }

        private GenericResponse<ServerConfiguration, ServerSelectError> Convert(GenericResponse<object, ServerGetError> e)
        {
            GenericResponse<ServerConfiguration, ServerSelectError> response = new GenericResponse<ServerConfiguration, ServerSelectError>();
            response.Message = e.Message;

            object o = e.Tag;
            if (o != null)
            {
                try
                {
                    JToken token = o as JToken;
                    if (token != null)
                    {
                        response.Tag = token.ToObject<ServerConfiguration>();
                    }
                    else
                    {
                        response.Tag = o as ServerConfiguration;
                    }
                }
                catch (Exception) { }
            }

            response.Code = unchecked((ServerSelectError)e.Code);
            return response;
        }

        [HttpGet("Get")]
        public GenericResponse<object, ServerGetError> Get()
        {
            return InternalGet(false);
        }

        [HttpGet("GetAll")]
        public GenericResponse<object, int> GetAll()
        {
            var context = this.HttpContext;
            return new GenericResponse<object, int>()
            {
                Code = 0,
                Tag = ServiceObjectContainer.Get<IEntryService>().GetCacheContentToken(context.GetQueryValue("platform"), context.GetQueryValue("paid"), EntryCacheCategories.ServersList)
            };
        }

        [HttpGet("Select")]
        public GenericResponse<ServerConfiguration, ServerSelectError> Select()
        {
            StatisticsController.GetDefaultController().AddCounter(PlatformUserSelectServer_Start);
            StatisticsController.GetDefaultController().StartStopWatch(PlatformUserSelectServer_Request);

            GenericResponse<ServerConfiguration, ServerSelectError> response = InternalSelectServer();
            if (response.Code == ServerSelectError.Success)
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserSelectServer_None);
            }
            else
            {
                StatisticsController.GetDefaultController().AddCounter(PlatformUserSelectServer_Error);
            }

            StatisticsController.GetDefaultController().StopStopWatch(PlatformUserSelectServer_Request);
            return response;
        }

        private static GenericResponse<object, TEnum> NewResponse<TEnum>(TEnum e) where TEnum : Enum
        {
            return new GenericResponse<object, TEnum>()
            {
                Code = e,
                Message = e.GetDescription()
            };
        }

        [HttpGet("Add")]
        public GenericResponse<object, ServerAddError> Add(ServerAddInfo model)
        {
            ServerAddError error = ServiceObjectContainer.Get<IEntryService>().Add(model);
            return NewResponse(error);
        }
    }
}