namespace GVMServer.Ns.Net
{
    using System;
    using System.Net.Sockets;
    using GVMServer.Net;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Model;

    public unsafe class NsSocket : SocketClient, ISocket
    {
        private NsTimer m_oLinkHeartbeatTimer = null;
        private Ns m_oCredentials = null;
        private ApplicationType m_eApplicationType = ApplicationType.ApplicationType_Namespace;

        public Guid Id { get; set; }

        public ApplicationType ApplicationType
        {
            get
            {
                Ns credentials = this.m_oCredentials;
                if (credentials == null)
                {
                    return this.m_eApplicationType;
                }
                return credentials.ApplicationType;
            }
            set
            {
                Ns credentials = this.m_oCredentials;
                if (credentials != null)
                {
                    credentials.ApplicationType = value;
                }
                this.m_eApplicationType = value;
            }
        }

        public ISocketHandler SocketHandler { get; set; }

        public override bool Available => base.Available && this.IsOpen;

        public bool IsOpen { get; private set; } = false;

        public virtual Ns Credentials => this.m_oCredentials;

        public virtual bool Authenticationing { get; private set; }

        public bool IsClient { get; } = false;

        public object Tag { get; set; }

        protected internal NsSocket(ISocketHandler handler, ApplicationType applicationType, Guid id)
        {
            this.SocketHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.IsClient = true;
            this.Id = id;
            this.ApplicationType = applicationType;
        }

        protected internal NsSocket(NsListener listener, Socket socket) : base(listener, socket)
        {
            this.SocketHandler = listener.SocketHandler;
        }

        protected override void OnMessage(SocketMessage e)
        {
            if (this.IsClient)
            {
                int error = 0;
                do
                {
                    Commands commands = (Commands)e.CommandId;
                    if (!this.IsOpen)
                    {
                        if (commands != Commands.Commands_Authentication)
                        {
                            error = -1;
                            break;
                        }

                        AuthenticationResponse response = Message.Deserialize<AuthenticationResponse>(e);
                        if (response.Code != Error.Error_Success)
                        {
                            error = -2;
                            break;
                        }
                        else
                        {
                            var credentials = response.Credentials;
                            if (credentials == null)
                            {
                                error = -3;
                                break;
                            }

                            lock (this)
                            {
                                this.IsOpen = true;
                                this.Authenticationing = false;
                                this.m_oCredentials = response.Credentials;
                            }

                            this.OnOpen(EventArgs.Empty);
                        }
                    }
                    else if (commands == Commands.Commands_Authentication)
                    {
                        error = -4;
                    }
                    else if (commands == Commands.Commands_LinkHeartbeat)
                    {
                        error = 1;
                    }
                } while (false);
                if (error < 0)
                {
                    this.Abort();
                }
                else if (error == 0)
                {
                    this.ProcessMessage(e);
                }
            }
            else
            {
                int error = 0;
                do
                {
                    Commands commands = (Commands)e.CommandId;
                    if (!this.IsOpen)
                    {
                        if (commands != Commands.Commands_Authentication)
                        {
                            error = -1;
                            break;
                        }

                        lock (this)
                        {
                            if (this.Authenticationing)
                            {
                                error = -2;
                                break;
                            }

                            this.Authenticationing = true;
                        }

                        AuthenticationRequest authentication = Message.Deserialize<AuthenticationRequest>(message: e);
                        if (this.ProcessAuthentication(ackNo: e.SequenceNo, authentication: authentication, credentials: (credentials) =>
                        {
                            if (credentials == null)
                            {
                                this.Abort();
                            }
                            else
                            {
                                lock (this)
                                {
                                    this.m_oCredentials = credentials;
                                    this.IsOpen = true;
                                    this.Authenticationing = false;
                                }

                                this.OnOpen(EventArgs.Empty);
                            }
                        }))
                        {
                            error = 1;
                        }
                        else
                        {
                            error = -3;
                        }
                    }
                    else if (commands == Commands.Commands_Authentication)
                    {
                        error = -4;
                    }
                    else if (commands == Commands.Commands_LinkHeartbeat)
                    {
                        error = 3;
                        if (this.IsOpen)
                        {
                            LinkHeartbeat linkHeartbeat = Message.Deserialize<LinkHeartbeat>(message: e);
                            this.ProcessLinkHeartbeat(ackNo: e.SequenceNo, heartbeat: linkHeartbeat);
                        }
                    }
                } while (false);
                if (error < 0)
                {
                    this.Abort();
                }
                else if (error == 0)
                {
                    this.ProcessMessage(e);
                }
            }
        }

        protected virtual bool ProcessAuthentication(long ackNo, AuthenticationRequest authentication, Action<Ns> credentials) // 对服务器登入进行鉴权
        {
            return this.SocketHandler.ProcessAuthentication(this, ackNo, authentication, credentials);
        }

        protected virtual bool ProcessLinkHeartbeat(long ackNo, LinkHeartbeat heartbeat)
        {
            return this.SocketHandler.ProcessLinkHeartbeat(this, ackNo, heartbeat);
        }

        public virtual bool Ack(Commands commands, long ackNo, byte[] buffer = null, int offset = 0, int length = ~0)
        {
            if (offset < 0)
            {
                return false;
            }

            if (length < 0)
            {
                if (buffer == null)
                {
                    length = 0;
                }
                else
                {
                    length = buffer.Length - offset;
                }
            }

            int boundary = 0;
            if (buffer != null)
            {
                boundary = buffer.Length;
            }

            if ((offset + length) > boundary)
            {
                return false;
            }

            if (!base.Available)
            {
                return false;
            }

            return this.Send(new SocketMessage(this, unchecked((ushort)commands), ackNo, 0, buffer, offset, length));
        }

        public virtual bool Send(Message message)
        {
            if (message == null)
            {
                return false;
            }

            if (!base.Available)
            {
                return false;
            }

            SocketMessage packet = new SocketMessage(this,
                unchecked((ushort)message.CommandId),
                message.SequenceNo,
                0,
                message.Payload.Buffer, message.Payload.Offset, message.Payload.Length);
            return this.Send(packet);
        }

        protected virtual bool InitiateAuthenticationAsync()
        {
            return this.SocketHandler.InitiateAuthenticationAsync(this);
        }

        protected virtual bool InitiateReportLinkHeartbeat()
        {
            return this.SocketHandler.InitiateReportLinkHeartbeat(this);
        }

        protected virtual void ProcessAccept(EventArgs e)
        {
            if (!this.SocketHandler.ProcessAccept(this))
            {
                this.Abort();
            }
            else
            {
                base.OnOpen(e);
                this.InitiateReportLinkHeartbeat();
            }
        }

        protected virtual void ProcessMessage(SocketMessage message)
        {
            if (!this.SocketHandler.ProcessMessage(this, message))
            {
                this.Abort();
            }
            else
            {
                base.OnMessage(message);
            }
        }

        protected virtual void ProcessAbort(EventArgs e)
        {
            this.StopLinkHeartbeat();
            this.SocketHandler.ProcessAbort(this);
        }

        protected override void OnOpen(EventArgs e)
        {
            bool kEstablished = false;
            lock (this)
            {
                kEstablished = this.IsOpen;
            }
            if (kEstablished)
            {
                this.StartLinkHeartbeat();
                this.ProcessAccept(e);
            }
            else if (this.IsClient)
            {
                lock (this)
                {
                    this.IsOpen = false;
                    this.Authenticationing = true;
                }
                if (!this.InitiateAuthenticationAsync())
                {
                    this.Abort();
                }
            }
        }

        private void StartLinkHeartbeat()
        {
            lock (this)
            {
                NsTimer oLinkHeartbeatTimer = this.m_oLinkHeartbeatTimer;
                if (oLinkHeartbeatTimer == null)
                {
                    this.m_oLinkHeartbeatTimer =
                        oLinkHeartbeatTimer = new NsTimer(1000);
                    oLinkHeartbeatTimer.Tick += (sender, ex) => this.InitiateReportLinkHeartbeat();
                    oLinkHeartbeatTimer.Start();
                }
            }
        }

        private void StopLinkHeartbeat()
        {
            NsTimer oLinkHeartbeatTimer = null;
            lock (this)
            {
                oLinkHeartbeatTimer = this.m_oLinkHeartbeatTimer;
                if (oLinkHeartbeatTimer != null)
                {
                    this.m_oLinkHeartbeatTimer = null;
                }
            }
            if (oLinkHeartbeatTimer != null)
            {
                oLinkHeartbeatTimer.Stop();
                oLinkHeartbeatTimer.Dispose();
            }
        }

        protected override void OnClose(EventArgs e)
        {
            this.ProcessAbort(e);
            base.OnClose(e);
        }

        protected override void OnError(EventArgs e)
        {
            this.ProcessAbort(e);
            base.OnError(e);
        }

        public override string ToString()
        {
            return $"NsSocket@{this.ApplicationType}/{this.Id}.{unchecked((uint)this.GetHashCode())}";
        }
    }
}
