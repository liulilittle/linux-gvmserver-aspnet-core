namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using GVMServer.Ns.Enum;

    public class SocketMvhRequest
    {
        public Message Message { get; }

        public Commands CommandId => Message.CommandId;

        public long SequenceNo => Message.SequenceNo;

        public SocketMvhContext Context { get; }

        public SocketMvhRequest(SocketMvhContext context, Message message)
        {
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual T Read<T>()
        {
            return Message.Deserialize<T>(this.Context?.GetHandlerAttribute()?.SerializationMode);
        }
    }

    public class SocketMvhResponse
    {
        public Commands CommandId { get; set; }

        public long SequenceNo { get; set; }

        public SocketMvhContext Context { get; set; }

        public SocketMvhResponse(SocketMvhContext context, Commands commandId, long sequenceNo)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.CommandId = commandId;
            this.SequenceNo = sequenceNo;
        }

        public bool Write(object o, int ackTimeout = 0, int ackRetransmission = 0, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue)
        {
            byte[] buffer = o.Serialize(this.Context?.GetHandlerAttribute()?.SerializationMode);
            if (buffer == null)
            {
                buffer = BufferSegment.Empty;
            }
            return this.Write(buffer, 0, buffer.Length, ackTimeout, ackRetransmission, ackCommands);
        }

        public bool Write(Stream stream, int ackTimeout = 0, int ackRetransmission = 0, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue)
        {
            byte[] buffer = null;
            if (stream != null)
            {
                if (stream is MemoryStream ms)
                {
                    buffer = ms.ToArray();
                }
                else
                {
                    int length = Convert.ToInt32(stream.Length - stream.Position);
                    if (length > 0)
                    {
                        long position = 0;
                        if (stream.CanSeek)
                        {
                            position = stream.Position;
                        }

                        buffer = new byte[length];
                        stream.Read(buffer, 0, length);

                        if (stream.CanSeek)
                        {
                            stream.Position = position;
                        }
                    }
                }
            }
            if (buffer == null)
            {
                buffer = BufferSegment.Empty;
            }
            return this.Write(buffer, 0, buffer.Length, ackTimeout, ackRetransmission, ackCommands);
        }

        public bool Write(byte[] buffer, int offset, int length, int ackTimeout = 0, int ackRetransmission = 0, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue)
        {
            BufferSegment payload = buffer.ToBufferSegment(offset, length);
            return this.Write(payload, ackTimeout, ackRetransmission, null, ackCommands);
        }

        public virtual bool Write(BufferSegment message, int ackTimeout = 0, int ackRetransmission = 0, RetransmissionEvent ackEvent = null, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue)
        {
            if (ackRetransmission < 0)
            {
                throw new ArgumentOutOfRangeException("The value of ackRetransmission is not allowed to be less than zero");
            }

            SocketMvhContext context = this.Context;
            if (context == null)
            {
                return false;
            }

            var socket = context.GetSocket();
            if (socket == null)
            {
                return false;
            }

            ISocketChannelManagement management = context.GetChannelManagement();
            SocketMvhRetransmission retransmission = management?.Retransmission;
            if (retransmission == null)
            {
                throw new InvalidOperationException("The current context state does not support retransmission control you should not use the retransmission mechanism");
            }

            Message packet = new Message(message)
            {
                CommandId = this.CommandId,
                SequenceNo = this.SequenceNo
            };
            if (ackTimeout > 0)
            {
                if (ackCommands == SocketMvhHandlerAttribute.DefaultValue)
                {
                    ackCommands = packet.CommandId;
                }
                return retransmission.AddRetransmission(
                       applicationType: context.ApplicationType,
                       id: context.Id,
                       ackMessage: packet,
                       ackTimeout: ackTimeout,
                       ackRetransmission: ackRetransmission,
                       ackEvent: ackEvent,
                       ackCommands: ackCommands,
                       exception: out Exception exception);
            }
            else
            {
                return socket.Send(packet);
            }
        }
    }

    public class SocketMvhContext
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ISocketChannelManagement m_poChannelManagement;

        public SocketMvhRequest Request { get; }

        public SocketMvhResponse Response { get; }

        public virtual ISocket GetSocket()
        {
            var socket = m_poChannelManagement?.GetChannel(this.ApplicationType, this.Id);
            return socket;
        }

        public ISocketChannelManagement GetChannelManagement() => this.m_poChannelManagement;

        public Guid Id { get; }

        public ApplicationType ApplicationType { get; }

        public bool IsSupportRetransmission => null != this.GetChannelManagement()?.Retransmission;

        public virtual SocketMvhHandlerAttribute GetHandlerAttribute()
        {
            var channelManagement = this.m_poChannelManagement;
            if (channelManagement == null)
            {
                return null;
            }
            var request = this.Request;
            if (request == null)
            {
                return null;
            }
            return channelManagement.HandlerContainer?.GetHandlerAttribute(this.ApplicationType, request.CommandId);
        }

        public SocketMvhContext(ISocketChannelManagement channelManagement, ISocket socket, Message message)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            this.m_poChannelManagement = channelManagement ?? throw new ArgumentNullException(nameof(socket));
            this.Id = socket.Id;
            this.ApplicationType = socket.ApplicationType;

            this.Request = this.CreateRequest(socket, message);
            this.Response = this.CreateResponse(socket, message);
        }

        protected virtual SocketMvhResponse CreateResponse(ISocket socket, Message message)
        {
            return new SocketMvhResponse(this, message.CommandId, message.SequenceNo);
        }

        protected virtual SocketMvhRequest CreateRequest(ISocket socket, Message message)
        {
            return new SocketMvhRequest(this, message);
        }
    }
}
