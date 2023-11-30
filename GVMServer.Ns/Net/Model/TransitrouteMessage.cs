namespace GVMServer.Ns.Net.Model
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using GVMServer.Ns.Enum;

    public unsafe class TransitrouteMessage : EventArgs
    {
        public Guid Id { get; set; }

        public ApplicationType ApplicationType { get; set; }

        public Message Message { get; set; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TransitroutePacketStruct
        {
            public long SequenceNo { get; set; }

            public Commands CommandId { get; set; }

            public ApplicationType ApplicationType { get; set; }

            public Guid Id { get; set; }
        }

        public virtual byte[] ToArray()
        {
            Message payload_message = this.Message;
            if (payload_message == null)
            {
                throw new InvalidOperationException("This operation is not allowed without a payload Message currently in the object");
            }

            BufferSegment payload_segment = payload_message.Payload;
            int message_length = sizeof(TransitroutePacketStruct);
            if (payload_segment != null)
            {
                message_length += payload_segment.Length;
            }

            byte[] message_data = new byte[message_length];
            fixed (byte* pinned = message_data)
            {
                TransitroutePacketStruct* span_ptr = (TransitroutePacketStruct*)pinned;
                span_ptr->CommandId = payload_message.CommandId;
                span_ptr->SequenceNo = payload_message.SequenceNo;
                span_ptr->Id = this.Id;
                span_ptr->ApplicationType = this.ApplicationType;
            }

            using (MemoryStream ms = new MemoryStream(message_data))
            {
                ms.Seek(sizeof(TransitroutePacketStruct), SeekOrigin.Begin);
                if (payload_segment != null)
                {
                    payload_segment.CopyTo(ms);
                }
            }
            return message_data;
        }

        public static TransitrouteMessage From(BufferSegment buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            int message_length = buffer.Length - sizeof(TransitroutePacketStruct);
            if (message_length < 0)
            {
                return null;
            }

            TransitrouteMessage message = null;
            buffer.UnsafeAddrOfPinnedArrayElement((pinned) =>
            {
                TransitroutePacketStruct* span_ptr = (TransitroutePacketStruct*)pinned;
                BufferSegment message_data = Message.Empty;
                if (message_length > 0)
                {
                    message_data = new BufferSegment(buffer.Buffer, buffer.Offset + sizeof(TransitroutePacketStruct), message_length);
                }
                message = new TransitrouteMessage()
                {
                    ApplicationType = span_ptr->ApplicationType,
                    Id = span_ptr->Id,
                    Message = new Message(message_data)
                    {
                        CommandId = span_ptr->CommandId,
                        SequenceNo = span_ptr->SequenceNo,
                    },
                };
            });
            return message;
        }
    }
}
