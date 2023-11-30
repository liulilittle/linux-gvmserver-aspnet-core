namespace GVMServer.Ns.Net
{
    using System;
    using System.IO;
    using GVMServer.Net;
    using GVMServer.Ns.Enum;
    using GVMServer.Serialization;

    public unsafe class Message : EventArgs
    {
        public new static readonly BufferSegment Empty = new BufferSegment(BufferSegment.Empty);

        public long SequenceNo { get; set; }

        public Commands CommandId { get; set; }

        public BufferSegment Payload { get; }

        public object Tag { get; set; }

        public Message(byte[] payload)
        {
            if (payload == null || payload.Length <= 0)
            {
                this.Payload = Empty;
            }
            else
            {
                this.Payload = new BufferSegment(payload);
            }
        }

        public Message(BufferSegment payload)
        {
            this.Payload = payload ?? Empty;
        }

        public static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return BufferSegment.Empty;
            }

            return BinaryFormatter.Serialize(o, false);
        }

        public static T Deserialize<T>(SocketMessage message)
        {
            if (message == null)
            {
                return default(T);
            }

            return Deserialize<T>(message.GetBuffer(), Convert.ToInt32(message.GetOffset()), Convert.ToInt32(message.GetLength()));
        }

        public static T Deserialize<T>(BufferSegment message)
        {
            if (message == null)
            {
                return default(T);
            }

            return Deserialize<T>(message.Buffer, message.Offset, message.Length);
        }

        public static T Deserialize<T>(Message message)
        {
            return Deserialize<T>(message?.Payload);
        }

        public static T Deserialize<T>(byte[] buffer, int offset = 0, int length = ~0)
        {
            if (buffer == null)
            {
                return default(T);
            }

            if (offset < 0)
            {
                return default(T);
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

            if (length <= 0)
            {
                return default(T);
            }

            if ((offset + length) > buffer.Length)
            {
                return default(T);
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(buffer, offset, length, true))
                {
                    if (!stream.CanRead)
                    {
                        return default(T);
                    }

                    try
                    {
                        return BinaryFormatter.Deserialize<T>(stream, false);
                    }
                    catch (Exception)
                    {
                        return default(T);
                    }
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static long NewId()
        {
            return SocketMessage.NewId();
        }

        public static Message From(SocketMessage e)
        {
            if (e == null)
            {
                return null;
            }

            long length = e.GetLength();
            long offset = e.GetOffset();
            byte[] buffer = e.GetBuffer();

            if (offset < 0)
            {
                return null;
            }

            long boundary = 0;
            if (buffer != null)
            {
                boundary = buffer.Length;
            }

            if ((offset + length) > boundary)
            {
                return null;
            }

            BufferSegment segment = null;
            if (length <= 0)
            {
                segment = Message.Empty;
            }
            else
            {
                segment = new BufferSegment(buffer, Convert.ToInt32(offset), Convert.ToInt32(length));
            }

            return new Message(segment)
            {
                CommandId = (Commands)e.CommandId,
                SequenceNo = e.SequenceNo
            };
        }
    }
}
