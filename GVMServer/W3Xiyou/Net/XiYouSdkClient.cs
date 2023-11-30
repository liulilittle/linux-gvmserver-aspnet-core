namespace GVMServer.W3Xiyou.Net
{
    using System;
    using System.Net.Sockets;
    using GVMServer.Net;

    public unsafe class XiYouSdkClient : SocketClient
    {
        private bool m_established = false;
        private DateTime m_activitytime = DateTime.MinValue;

        public const string DEFAULT_PLATFORM = "xiyou";

        public override bool Available => this.m_established;

        public ushort Identifier { get; private set; }

        public byte MetadataToken { get; private set; }

        public string Nomenclature { get; private set; }

        public string Platform { get; private set; }

        protected internal XiYouSdkClient(SocketListener listener, Socket socket) : base(listener, socket)
        {

        }

        private bool ProcessEstablished(SocketMessage e)
        {
            if (e == null)
            {
                return false;
            }
            try
            {
                do
                {
                    long identifier = e.Identifier;
                    byte* buffer = (byte*)&identifier;

                    this.MetadataToken = buffer[0];
                    this.Identifier = *(ushort*)&buffer[1];
                } while (false);
                long count = e.GetLength();
                if ((count -= sizeof(ushort)) < 0)
                {
                    return false;
                }
                fixed (byte* pinned = e.GetBuffer())
                {
                    if (null == pinned)
                    {
                        return false;
                    }
                    byte* buf = pinned;
                    string[] contents = { string.Empty, string.Empty };
                    for (int i = 0; i < contents.Length; i++)
                    {
                        ushort len = *(ushort*)buf;
                        buf += +sizeof(ushort);
                        if (len == 0 || len == ushort.MaxValue)
                        {
                            if (i > 0)
                            {
                                break;
                            }
                            return false;
                        }
                        if ((count -= len) < 0)
                        {
                            if (i > 0)
                            {
                                break;
                            }
                            return false;
                        }
                        contents[i] = (new string((sbyte*)buf, 0, len) ?? string.Empty).TrimEnd('\x0');
                        buf += len;
                    }

                    this.Nomenclature = contents[0];
                    this.Platform = string.IsNullOrEmpty(contents[1]) ? DEFAULT_PLATFORM : contents[1];
                }
                this.m_established = true;
            }
            catch (Exception)
            {
                this.m_established = false;
                return false;
            }
            return true;
        }

        protected override void OnMessage(SocketMessage e)
        {
            XiYouSdkCommands commands = unchecked((XiYouSdkCommands)e.CommandId);
            int callType = 0;
            lock (this)
            {
                if (!this.m_established)
                {
                    if (commands != XiYouSdkCommands.XiYouSdkCommands_Established || !this.ProcessEstablished(e))
                    {
                        callType = 1;
                    }
                    else
                    {
                        callType = 3;
                    }
                }
                else if (commands != XiYouSdkCommands.XiYouSdkCommands_Established)
                {
                    callType = 2;
                }
            }
            if (callType == 1)
            {
                base.CloseOrAbort(true); // 此连接有问题
            }
            else
            {
                this.m_activitytime = DateTime.Now;
                if (callType == 2)
                {
                    if (commands != XiYouSdkCommands.XiYouSdkCommands_Heartbeat)
                    {
                        base.OnMessage(e);
                    }
                    else
                    {
                        SocketMessage message = new SocketMessage(this, (int)XiYouSdkCommands.XiYouSdkCommands_Heartbeat,
                            SocketMessage.NewId(), this.Identifier, null, 0, 0);
                        this.Send(message);
                    }
                }
                else if (callType == 3)
                {
                    this.OnOpen(EventArgs.Empty); // 链接建立成功
                }
            }
        }

        protected virtual int MaxSilencingTime
        {
            get
            {
                return 30000;
            }
        }

        protected internal virtual void DoEvents()
        {
            DateTime nowtime = DateTime.Now;
            double ticks = (nowtime - this.m_activitytime).TotalMilliseconds;
            if (ticks >= this.MaxSilencingTime)
            {
                this.CloseOrAbort(true);
            }
        }
    }
}
