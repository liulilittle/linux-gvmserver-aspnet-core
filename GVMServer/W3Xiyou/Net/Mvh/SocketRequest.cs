namespace GVMServer.W3Xiyou.Net.Mvh
{
    using GVMServer.Net;
    using GVMServer.Serialization;
    using System;
    using System.IO;

    public class SocketRequest
    {
        private readonly SocketMessage message;
        private readonly SocketContext context;

        public ushort CommandId
        {
            get;
            private set;
        }

        public long SequenceNo
        {
            get;
            private set;
        }

        public long Identifier
        {
            get;
            private set;
        }

        internal SocketRequest(SocketContext context, SocketMessage message)
        {
            this.context = context;
            this.message = message;

            this.CommandId = message.CommandId;
            this.SequenceNo = message.SequenceNo;
            this.Identifier = message.Identifier;
        }

        public MemoryStream GetMessage()
        {
            SocketMessage message = this.message;
            return new MemoryStream(message.GetBuffer(), Convert.ToInt32(message.GetOffset()), Convert.ToInt32(message.GetLength()));
        }

        public T Read<T>()
        {
            return BinaryFormatter.Deserialize<T>(message.GetBuffer(), false);
        }
    }
}
