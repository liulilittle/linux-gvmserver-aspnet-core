namespace GVMServer.Ns.Deployment
{
    using System;
    using System.Runtime.InteropServices;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Model;
    using GVMServer.Ns.Net.Mvh;
    using Msg = GVMServer.Ns.Net.Message;
    using Ns = GVMServer.Ns.Functional.Ns;

    /// <summary>
    /// 虚拟套接字通道（此实例用于构建框架所需要的虚拟链路通道）
    /// </summary>
    public unsafe class ApplicationVirtualSocket : ISocket, IDisposable
    {
        private readonly ISocket m_poPhysicalChannel;
        private bool m_closeing = false;
        private bool m_disposed = false;

        public ApplicationVirtualSocket(ISocketChannelManagement channelManagement,
            ISocket physicalChannel, ApplicationType applicationType, Guid id, Ns credentials)
        {
            this.m_poPhysicalChannel = physicalChannel ?? throw new ArgumentNullException(nameof(physicalChannel));
            this.Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this.ChannelManagement = channelManagement ?? throw new ArgumentNullException(nameof(channelManagement));

            this.Id = id;
            this.ApplicationType = applicationType;
        }

        ~ApplicationVirtualSocket()
        {
            this.Dispose();
        }

        public event EventHandler<Msg> Message;
        public event EventHandler<EventArgs> Accept;
        public event EventHandler<EventArgs> Abort;

        public virtual bool Available
        {
            get
            {
                bool available = false;
                lock (this)
                    available = !this.m_closeing;
                return available && m_poPhysicalChannel.Available; 
            }
        }

        public Guid Id { get; }

        public Ns Credentials { get; }

        public ApplicationType ApplicationType { get; }

        public ISocketChannelManagement ChannelManagement { get; }

        public virtual void Close()
        {
            bool aborting = false;
            lock (this)
            {
                if (!m_closeing)
                {
                    m_closeing = true;
                    aborting = true;
                }
            }
            if (aborting)
            {
                this.OnAbort(EventArgs.Empty);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TransitroutePacketStruct
        {
            public TransitrouteMessage.TransitroutePacketStruct Destination;
            public TransitrouteMessage.TransitroutePacketStruct Sources;
        }

        public virtual bool Send(Msg message)
        {
            if (message == null)
            {
                return false;
            }
            if (message.CommandId == Commands.Commands_RoutingMessage)
            {
                TransitrouteMessage poTransitroute = new TransitrouteMessage()
                {
                    Message = message,
                    Id = this.Id,
                    ApplicationType = this.ApplicationType,
                };
                return this.m_poPhysicalChannel.Send(new Msg(poTransitroute.ToArray().ToBufferSegment())
                {
                    SequenceNo = Msg.NewId(),
                    CommandId = Commands.Commands_Transitroute,
                });
            }
            else
            {
                byte[] transitroute = new byte[sizeof(TransitroutePacketStruct) + message.Payload.Length];
                fixed (byte* pinned = transitroute)
                {
                    TransitroutePacketStruct* packet = (TransitroutePacketStruct*)pinned;
                    packet->Destination.ApplicationType = this.ApplicationType;
                    packet->Destination.Id = this.Id;
                    packet->Destination.SequenceNo = Msg.NewId();
                    packet->Destination.CommandId = Commands.Commands_Transitroute;

                    packet->Sources.ApplicationType = this.m_poPhysicalChannel.ApplicationType;
                    packet->Sources.Id = this.m_poPhysicalChannel.Id;
                    packet->Sources.CommandId = message.CommandId;
                    packet->Sources.SequenceNo = message.SequenceNo;

                    Marshal.Copy(message.Payload.Buffer, message.Payload.Offset, (IntPtr)(pinned + sizeof(TransitroutePacketStruct)), message.Payload.Length);
                }
                return this.m_poPhysicalChannel.Send(new Msg(transitroute.ToBufferSegment())
                {
                    SequenceNo = Msg.NewId(),
                    CommandId = Commands.Commands_Transitroute,
                });
            }
        }

        public virtual ISocket GetPhysicalChannel()
        {
            return m_poPhysicalChannel;
        }

        protected internal void OnMessage(Msg message)
        {
            this.Message?.Invoke(this, message);
        }

        protected internal void OnAbort(EventArgs e)
        {
            Console.WriteLine($"AVS Abort {this}");
            this.Abort?.Invoke(this, e);
        }

        protected internal void OnAccept(EventArgs e)
        {
            Console.WriteLine($"AVS Accept {this}");
            this.Accept?.Invoke(this, e);
        }

        public override string ToString()
        {
            return $"ApplicationVirtualSocket@{this.ApplicationType}/{this.Id}.{unchecked((uint)this.GetHashCode())}";
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!m_disposed)
                {
                    this.Close();
                    this.Message = null;
                    this.Accept = null;
                    this.Abort = null;
                }
            }
            GC.SuppressFinalize(this);
        }
    }

}
