using GVMServer.Net;
using GVMServer.Serialization;
using System;

namespace GVMServer.W3Xiyou.Net.Mvh
{
    public class SocketResponse
    {
        private readonly SocketMessage message;
        private readonly SocketContext context;

        public ushort CommandId
        {
            get;
            set;
        }

        public long SequenceNo
        {
            get;
            set;
        }

        public long Identifier
        {
            get;
            set;
        }

        internal SocketResponse(SocketContext context, SocketMessage message)
        {
            this.context = context;
            this.message = message;

            this.CommandId = message.CommandId;
            this.SequenceNo = message.SequenceNo;
            this.Identifier = message.Identifier;
        }

        public bool Write(object o)
        {
            byte[] buffer = BinaryFormatter.Serialize(o, false);
            return this.Write(buffer);
        }

        public bool Write(byte[] buffer)
        {
            if (buffer == null)
            {
                return false;
            }
            return this.Write(buffer, buffer.Length);
        }

        public bool Write(byte[] buffer, int count)
        {
            return this.Write(buffer, 0, count);
        }

        public bool Write(byte[] buffer, int offset, int count)
        {
            try
            {
                if (this.message == null)
                {
                    return false;
                }
                ISocketClient socket = this.message.GetClient();
                if (socket == null)
                {
                    return false;
                }
                SocketMessage message = new SocketMessage(socket, this.CommandId, this.SequenceNo,
                    this.Identifier, buffer, offset, count);
                return socket.Send(message);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
