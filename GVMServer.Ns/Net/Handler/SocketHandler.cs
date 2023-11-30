namespace GVMServer.Ns.Net.Handler
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using GVMServer.DDD.Service;
    using GVMServer.Net;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using GVMServer.Ns.Net.Model;
    using GVMServer.W3Xiyou.Docking;

    public class SocketHandler : ISocketHandler
    {
        protected class HandlingControlContext
        {
            public int addRef = 0;
            public DateTime lastHandlingLinkHeartbeatTime = DateTime.MinValue;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ConcurrentDictionary<string, HandlingControlContext> m_poHandlingControl = new ConcurrentDictionary<string, HandlingControlContext>();

        public BaseApplication Application => ServiceObjectContainer.Get<BaseApplication>();

        protected virtual bool LookupCredentialsAsync(Guid guid, Action<GenericResponse<Error, Ns>> credentials)
        {
            string rawUri = ServerApplication.GetWebHost(this.Application.GetConfiguration());
            if (string.IsNullOrEmpty(rawUri))
            {
                return false;
            }
            try
            {
                rawUri += "api/ns/get?";
                rawUri += $"guid={guid}";
                rawUri = new Uri(rawUri).ToString();

                XiYouUtility.GetFromUrlAsync<GenericResponse<Error, Ns>>(rawUri, 3000, (error, e) => credentials?.Invoke(e));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual bool InitiateAuthenticationAsync(NsSocket socket)
        {
            AuthenticationRequest authentication = new AuthenticationRequest
            {
                Id = socket.Id,
                ApplicationType = socket.ApplicationType
            };
            return socket.Ack(Commands.Commands_Authentication, 0, authentication.Serialize());
        }

        public virtual bool InitiateReportLinkHeartbeat(NsSocket socket)
        {
            LinkHeartbeat heartbeat = LinkHeartbeat.Capture();
            if (heartbeat == null)
            {
                return false;
            }

            return socket.Ack(Commands.Commands_LinkHeartbeat, Message.NewId(), heartbeat.Serialize());
        }

        public virtual bool ProcessAuthentication(NsSocket socket, long ackNo, AuthenticationRequest authentication, Action<Ns> credentials)
        {
            if (socket == null)
            {
                return false;
            }
            AuthenticationResponse response = new AuthenticationResponse
            {
                Code = Error.Error_Success,
            };
            ApplicationType applicationType = this.Application.ApplicationType;
            Ns certificate = null;
            try
            {
                if (authentication == null)
                {
                    response.Code = Error.Error_TheAuthenticationRequestDataModelProvidedIsAnEmptyReferences;
                    return false;
                }

                if (!Enum.IsDefined(typeof(ApplicationType), authentication.ApplicationType))
                {
                    response.Code = Error.Error_UndefinedInputTheApplicationTypeEnum;
                    return false;
                }

                if (applicationType == ApplicationType.ApplicationType_Namespace)
                {
                    certificate = ServiceObjectContainer.Get<NsService>().FindOrDefault(authentication.Id, out Error error);
                    if (certificate == null)
                    {
                        response.Code = Error.Error_TheIdProvidingTheAuthenticationIsInvalid;
                        return false;
                    }

                    if (certificate.ApplicationType != authentication.ApplicationType)
                    {
                        response.Code = Error.Error_TheApplicationTypeIsNotTheSameAsTheRegistrationType;
                        return false;
                    }

                    response.Credentials = certificate;
                    socket.Id = authentication.Id;
                    socket.ApplicationType = authentication.ApplicationType;
                    return true;
                }
                else
                {
                    return LookupCredentialsAsync(authentication.Id, (e) =>
                    {
                        bool success = false;
                        if (e == null)
                        {
                            response.Code = Error.Error_ThereWasAFailureInTheAccessApiInterfaceAndItCouldBeATimeoutOrAProblemWithTheSerializedResponse;
                        }
                        else if (e.Code != Error.Error_Success)
                        {
                            response.Code = e.Code;
                        }
                        else if ((certificate = e.Tag) == null)
                        {
                            response.Code = Error.Error_NsInstanceModelIsNullReferences;
                        }
                        else
                        {
                            if (authentication.ApplicationType != certificate.ApplicationType)
                            {
                                response.Code = Error.Error_TheApplicationTypeIsNotTheSameAsTheRegistrationType;
                            }
                            else
                            {
                                success = true;
                                response.Credentials = certificate;
                                socket.Id = authentication.Id;
                                socket.ApplicationType = authentication.ApplicationType;
                            }
                        }
                        bool ack = socket.Ack(Commands.Commands_Authentication, ackNo, Message.Serialize(response));
                        if (success)
                        {
                            credentials?.Invoke(certificate);
                        }
                        if (!ack || !success)
                        {
                            socket.Abort();
                        }
                    });
                }
            }
            finally
            {
                if (applicationType == ApplicationType.ApplicationType_Namespace)
                {
                    bool ack = socket.Ack(Commands.Commands_Authentication, ackNo, Message.Serialize(response));
                    if (certificate != null)
                    {
                        credentials?.Invoke(certificate);
                    }
                    if (!ack)
                    {
                        socket.Abort();
                    }
                }
            }
        }

        public virtual bool ProcessLinkHeartbeat(NsSocket socket, long ackNo, LinkHeartbeat heartbeat)
        {
            if (socket == null || heartbeat == null)
            {
                return false;
            }

            if (!this.AllowHandlingLinkHeartbeat(socket.ApplicationType, socket.Id))
            {
                return false;
            }

            ApplicationType applicationType = this.Application.ApplicationType;
            if (applicationType == ApplicationType.ApplicationType_Namespace)
            {
                ServiceObjectContainer.Get<NsLoadbalancing>().AddSampling(socket.ApplicationType, socket.Id, heartbeat);
            }
            return true;
        }

        protected virtual bool AllowHandlingLinkHeartbeat(ApplicationType applicationType, Guid id)
        {
            string key = $"{applicationType}.{id}";
            lock (this.m_poHandlingControl)
            {
                this.m_poHandlingControl.TryGetValue(key, out HandlingControlContext context);
                if (context == null)
                {
                    return false;
                }

                return DateTime.Now.Subtract(context.lastHandlingLinkHeartbeatTime).TotalSeconds >= 1;
            }
        }

        protected virtual HandlingControlContext AllocHandlingControl(ApplicationType applicationType, Guid id)
        {
            string key = $"{applicationType}.{id}";
            HandlingControlContext context = null;
            lock (this.m_poHandlingControl)
            {
                this.m_poHandlingControl.TryGetValue(key, out context);
                if (context == null)
                {
                    context = new HandlingControlContext();
                    this.m_poHandlingControl[key] = context;
                }
            }
            if (context != null)
            {
                Interlocked.Increment(ref context.addRef);
            }
            return context;
        }

        protected virtual void ReleaseHandlingControl(ApplicationType applicationType, Guid id)
        {
            string key = $"{applicationType}.{id}";
            lock (this.m_poHandlingControl)
            {
                if (this.m_poHandlingControl.TryGetValue(key, out HandlingControlContext context))
                {
                    if (context == null || Interlocked.Decrement(ref context.addRef) <= 0)
                    {
                        this.m_poHandlingControl.TryRemove(key, out HandlingControlContext context_xx);
                    }
                }
            }
        }

        public virtual bool ProcessAbort(NsSocket socket)
        {
            if (socket == null)
            {
                return false;
            }

            this.ReleaseHandlingControl(socket.ApplicationType, socket.Id);
            return true;
        }

        public virtual bool ProcessAccept(NsSocket socket)
        {
            if (socket == null)
            {
                return false;
            }

            var context = this.AllocHandlingControl(socket.ApplicationType, socket.Id);
            return context != null;
        }

        public virtual bool ProcessMessage(NsSocket socket, SocketMessage message)
        {
            return true;
        }
    }
}
