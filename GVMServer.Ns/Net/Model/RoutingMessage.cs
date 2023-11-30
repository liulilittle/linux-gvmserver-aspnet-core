namespace GVMServer.Ns.Net.Model
{
    using System;
    using System.IO;
    using System.Text;
    using GVMServer.Ns.Enum;
    using GVMServer.Serialization;

    public class RoutingMessage : EventArgs
    {
        public ApplicationType PeerApplicationType { get; set; }

        public ApplicationType SocketApplicationType { get; set; }

        public int PeekServerNo { get; set; }

        public string PeerPlatform { get; set; }

        public int SocketServerNo { get; set; }

        public string SocketPlatform { get; set; }

        public Commands CommandId { get; set; }

        public long SequenceNo { get; set; }

        public BufferSegment Payload { get; set; }

        public BufferSegment ToArray()
        {
            using (MemoryStream s = new MemoryStream())
            {
                Encoding encoding = BinaryFormatter.DefaultEncoding;
                using (BinaryWriter bw = new BinaryWriter(s, encoding))
                {
                    bw.Write((byte)this.PeerApplicationType);
                    bw.Write((byte)this.SocketApplicationType);
                    bw.Write(this.PeekServerNo);

                    string peerplatform = (this.PeerPlatform ?? string.Empty).TrimEnd().TrimStart().TrimEnd('\x9', '\n', '\r');
                    bw.Write((ushort)(peerplatform?.Length ?? 0));
                    if (!string.IsNullOrEmpty(peerplatform))
                    {
                        bw.Write(peerplatform);
                    }

                    bw.Write(this.SocketServerNo);

                    string socketplatform = (this.SocketPlatform ?? string.Empty).TrimEnd().TrimStart().TrimEnd('\x9', '\n', '\r');
                    bw.Write((ushort)(socketplatform?.Length ?? 0));
                    if (!string.IsNullOrEmpty(socketplatform))
                    {
                        bw.Write(socketplatform);
                    }

                    bw.Write((ushort)this.CommandId);
                    bw.Write(this.SequenceNo);

                    BufferSegment payload_data = this.Payload;
                    if (payload_data != null)
                    {
                        payload_data.CopyTo(s);
                    }

                    return new BufferSegment(s.GetBuffer(), Convert.ToInt32(s.Position));
                }
            }
        }

        public Message GetMessage()
        {
            return new Message(this.Payload)
            {
                CommandId = this.CommandId,
                SequenceNo = this.SequenceNo,
            };
        }

        public static RoutingMessage From(BufferSegment segment)
        {
            if (segment == null)
            {
                return null;
            }

            RoutingMessage message = new RoutingMessage();
            using (MemoryStream ms = new MemoryStream(segment.Buffer, segment.Offset, segment.Length, false))
            {
                Encoding encoding = BinaryFormatter.DefaultEncoding;
                using (BinaryReader br = new BinaryReader(ms, encoding))
                {
                    try
                    {
                        message.PeerApplicationType = (ApplicationType)br.ReadByte();
                        message.SocketApplicationType = (ApplicationType)br.ReadByte();

                        message.PeekServerNo = br.ReadInt32();
                        ushort platform_size = br.ReadUInt16();
                        message.PeerPlatform = (0 == platform_size ? string.Empty : encoding.GetString(br.ReadBytes(platform_size))).TrimEnd().TrimStart().TrimEnd('\x9', '\n', '\r'); ;

                        message.SocketServerNo = br.ReadInt32();
                        platform_size = br.ReadUInt16();
                        message.SocketPlatform = (0 == platform_size ? string.Empty : encoding.GetString(br.ReadBytes(platform_size))).TrimEnd().TrimStart().TrimEnd('\x9', '\n', '\r'); ;

                        message.CommandId = (Commands)br.ReadUInt16();
                        message.SequenceNo = br.ReadInt64();

                        int payload_length = Convert.ToInt32(segment.Length - ms.Position);
                        if (payload_length <= 0)
                        {
                            message.Payload = Message.Empty;
                        }
                        else
                        {
                            message.Payload = new BufferSegment(segment.Buffer, Convert.ToInt32(segment.Offset + ms.Position), payload_length);
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            return message;
        }
    }
}
