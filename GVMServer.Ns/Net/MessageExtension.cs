namespace GVMServer.Ns.Net
{
    using System;
    using System.IO;
    using GVMServer.Net;
    using GVMServer.Ns.Net.Mvh;
    using GVMServer.Serialization;
    using GVMServer.W3Xiyou.Docking;

    public static class MessageExtension
    {
        public static byte[] Serialize<T>(this T o, ObjectSerializationMode? mode = null)
        {
            if (typeof(T) == typeof(byte[]))
            {
                return (byte[])(object)o;
            }
            mode = mode ?? ObjectSerializationMode.BinaryFormatter;
            switch (mode)
            {
                case ObjectSerializationMode.Protobuf:
                    return ProtobufSerializer.Serialize(o);
            }
            return Message.Serialize(o);
        }

        public static T Deserialize<T>(this SocketMessage message, ObjectSerializationMode? mode = null)
        {
            if (message == null)
            {
                return default(T);
            }
            mode = mode ?? ObjectSerializationMode.BinaryFormatter;
            switch (mode)
            {
                case ObjectSerializationMode.Protobuf:
                    using (MemoryStream ms = new MemoryStream(message.GetBuffer(), Convert.ToInt32(message.GetOffset()), Convert.ToInt32(message.GetLength())))
                        return ProtobufSerializer.Deserialize<T>(ms);
            }
            return Message.Deserialize<T>(message);
        }

        public static T Deserialize<T>(this Message message, ObjectSerializationMode? mode = null)
        {
            if (message == null)
            {
                return default(T);
            }
            mode = mode ?? ObjectSerializationMode.BinaryFormatter;
            switch (mode)
            {
                case ObjectSerializationMode.Protobuf:
                    using (MemoryStream ms = new MemoryStream(message.Payload.Buffer, message.Payload.Offset, message.Payload.Length))
                        return ProtobufSerializer.Deserialize<T>(ms);
            }
            return Message.Deserialize<T>(message);
        }

        public static T Deserialize<T>(this byte[] buffer, int offset = 0, int length = ~0, ObjectSerializationMode? mode = null)
        {
            if (buffer == null)
            {
                buffer = BufferSegment.Empty;
            }
            mode = mode ?? ObjectSerializationMode.BinaryFormatter;
            switch (mode)
            {
                case ObjectSerializationMode.Protobuf:
                    using (MemoryStream ms = new MemoryStream(buffer, offset, length))
                        return ProtobufSerializer.Deserialize<T>(ms);
            }
            return Message.Deserialize<T>(buffer, offset, length);
        }

        public static BufferSegment ToBufferSegment(this byte[] buffer, int offset = 0, int length = ~0)
        {
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

            return new BufferSegment(buffer, offset, length);
        }

        public static string ToJson(this object o)
        {
            if (o == null || (o as string) == string.Empty)
            {
                return string.Empty;
            }
            return XiYouSerializer.SerializableJson(o) ?? string.Empty;
        }

        public static T FromJson<T>(this string json)
        {
            if (typeof(T) == typeof(string))
            {
                if (json == null)
                {
                    return default(T);
                }
                return (T)(object)json;
            }
            if (string.IsNullOrEmpty(json))
            {
                return default(T);
            }
            try
            {
                return XiYouSerializer.DeserializeJson<T>(json);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
