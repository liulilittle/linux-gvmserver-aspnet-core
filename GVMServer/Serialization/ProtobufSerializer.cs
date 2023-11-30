namespace GVMServer.Serialization
{
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using ProtoBuf;
    using ProtoBuf.Meta;

    public class ProtobufSerializer
    {
        public static string GetProto<T>() => GetProto<T>(ProtoSyntax.Proto2);

        public static string GetProto<T>(ProtoSyntax syntax) => Serializer.GetProto<T>(syntax);

        public static byte[] Serialize<T>(T instance)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize(ms, instance);
                return ms.ToArray();
            }
        }

        public static void Serialize<T>(Stream stream, T instance) => Serializer.Serialize(stream, instance);

        public static void Serialize<T>(XmlWriter xml, T instance) where T : IXmlSerializable
        {
            if (xml != null)
            {
                Serializer.Serialize(xml, instance);
            }
        }

        public static T Deserialize<T>(Stream stream) => stream == null ? default(T) : Serializer.Deserialize<T>(stream);

        public static T Deserialize<T>(XmlReader xml) where T : IXmlSerializable, new()
        {
            if (xml == null)
            {
                return default(T);
            }
            else
            {
                T o = new T();
                Serializer.Merge(xml, o);
                return o;
            }
        }
    }
}
