namespace GVMServer.Ns.Functional
{
    using System;
    using System.Security;
    using System.Collections.Generic;
    using System.Net;
    using GVMServer.DDD.Service;
    using GVMServer.Net.Web;
    using GVMServer.Net.Web.Mvc.Controller;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Model;
    using GVMServer.Ns.Net;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.W3Xiyou.Net;

    public class NsController : Controller
    {
        [SecurityCritical]
        private static Error GetApplicationType(HttpContext context, out ApplicationType applicationType)
        {
            applicationType = ApplicationType.ApplicationType_Namespace;
            string s = (context.Request.QueryString["ApplicationType"] ?? string.Empty).TrimStart().TrimEnd();
            if (string.IsNullOrEmpty(s))
            {
                return Error.Error_ApplicationTypeAreNotAllowedToBeEmptyOrFullBlankCharacterSet;
            }

            if (!int.TryParse(s, out int i))
            {
                return Error.Error_UnableToConvertStringToNumber;
            }

            ApplicationType outType = (ApplicationType)i;
            if (!Enum.IsDefined(typeof(ApplicationType), outType))
            {
                return Error.Error_UndefinedInputTheApplicationTypeEnum;
            }

            applicationType = outType;
            return Error.Error_Success;
        }

        [HttpGet("/api/ns/putonrecord")]
        public void PutOnRecord(HttpContext context)
        {
            GenericResponse<Error, Ns> response = new GenericResponse<Error, Ns>();
            response.Code = (int)Error.Error_Success;
            do
            {
                Ns ns = null;
                try
                {
                    ns = XiYouSerializer.DeserializeObject<Ns>(context.Request.QueryString);
                    if (ns == null)
                    {
                        response.Code = Error.Error_UnableToDeserializeRequestParameters;
                        break;
                    }
                }
                catch (Exception)
                {
                    response.Code = Error.Error_UnableToDeserializeRequestParameters;
                    break;
                }

                if (!Enum.IsDefined(typeof(ApplicationType), ns.ApplicationType))
                {
                    response.Code = Error.Error_UndefinedInputTheApplicationTypeEnum;
                    break;
                }

                ns.AddressMask = (ns.AddressMask ?? string.Empty).TrimStart().TrimEnd();
                if (string.IsNullOrEmpty(ns.AddressMask))
                {
                    response.Code = Error.Error_AddressMaskCannotBeASetOfEmptyOrFullWhiteSpaceCharacters;
                    break;
                }

                ns.PlatformName = (ns.PlatformName ?? string.Empty).TrimStart().TrimEnd();
                ns.INetAddress = (context.Request.RemoteEndPoint?.Address ?? IPAddress.Any).ToString();
                if (ns.ApplicationType == ApplicationType.ApplicationType_GameServer || ns.ApplicationType == ApplicationType.ApplicationType_CrossServer)
                {
                    if (ns.ServerNo <= 0)
                    {
                        response.Code = Error.Error_GameServerApplicationNotAllowInputServerNoLessOrEqualsZero;
                        break;
                    }

                    if (string.IsNullOrEmpty(ns.PlatformName))
                    {
                        ns.PlatformName = XiYouSdkClient.DEFAULT_PLATFORM;
                    }
                }

                response.Code = ServiceObjectContainer.Get<NsService>().PutOnRecord(ns);
                if (response.Code == Error.Error_Success)
                {
                    response.Tag = ns;
                }
            } while (false);
            context.Response.Write(response.ToJson());
        }

        [HttpGet("/api/ns/get")]
        public void Get(HttpContext context)
        {
            GenericResponse<Error, Ns> response = new GenericResponse<Error, Ns>();
            response.Code = Error.Error_Success;
            do
            {
                try
                {
                    string guid = (context.Request.QueryString["guid"] ?? string.Empty).TrimStart().TrimEnd();
                    if (string.IsNullOrEmpty(guid))
                    {
                        response.Code = Error.Error_NodeidAreNotAllowedToBeEmptyOrFullBlankCharacterSet;
                        break;
                    }
                    if (!Guid.TryParse(guid, out Guid id))
                    {
                        response.Code = Error.Error_TheSuppliedValueIsNotAValidGuidFormatStringSupportForExampleN_D_B_P_XGuidFormat;
                        break;
                    }
                    response.Tag = ServiceObjectContainer.Get<NsService>().FindOrDefault(id, out Error error);
                    if (error == Error.Error_Success && response.Tag == null)
                    {
                        response.Code = Error.Error_SuccessButTheContentAssociatedWithThisGuidCouldNotBeFound;
                    }
                    response.Code = error;
                }
                catch (Exception)
                {
                    response.Code = Error.Error_UnknowTheUnhandlingExceptionWarning;
                }
            } while (false);
            context.Response.Write(response.ToJson());
        }

        [HttpGet("/api/ns/lookup")]
        public void Lookup(HttpContext context)
        {
            GenericResponse<Error, NodeSample> response = new GenericResponse<Error, NodeSample>();
            response.Code = Error.Error_Success;
            try
            {
                response.Code = GetApplicationType(context, out ApplicationType applicationType);
                if (response.Code == Error.Error_Success)
                {
                    response.Code = ServiceObjectContainer.Get<NsLoadbalancing>().Lookup(applicationType, out NodeSample sampling);
                    response.Tag = sampling;
                }
            }
            finally
            {
                context.Response.Write(response.ToJson());
            }
        }

        [HttpGet("/api/ns/lookupall")]
        public void LookupAll(HttpContext context)
        {
            GenericResponse<Error, IEnumerable<NodeSample>> response = new GenericResponse<Error, IEnumerable<NodeSample>>();
            response.Code = Error.Error_Success;
            try
            {
                response.Code = GetApplicationType(context, out ApplicationType applicationType);
                if (response.Code == Error.Error_Success)
                {
                    response.Code = ServiceObjectContainer.Get<NsLoadbalancing>().LookupAll(applicationType, out IEnumerable<NodeSample> sampling);
                    response.Tag = sampling;
                }
            }
            finally
            {
                context.Response.Write(response.ToJson());
            }
        }
    }
}
