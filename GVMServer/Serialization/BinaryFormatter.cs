namespace GVMServer.Serialization
{
#pragma warning disable 0675, 0618
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using GVMServer.Utilities;

    public unsafe static partial class BinaryFormatter
    {
        [AttributeUsage(AttributeTargets.Property)]
        public class IgnoreAttribute : Attribute
        {

        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IList<Type> m_mapTabType = new Type[] 
        {
            typeof(double),
            typeof(float),
            typeof(long),
            typeof(ulong),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(char),
            typeof(int),
            typeof(uint),
            typeof(bool),
            typeof(DateTime),
            typeof(IPAddress),
            typeof(Guid),
        };
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IList<byte> m_mapTabSize = new byte[] 
        {
            (byte)sizeof(double),
            (byte)sizeof(float),
            (byte)sizeof(long),
            (byte)sizeof(ulong),
            (byte)sizeof(byte),
            (byte)sizeof(sbyte),
            (byte)sizeof(short),
            (byte)sizeof(ushort),
            (byte)sizeof(char),
            (byte)sizeof(int),
            (byte)sizeof(uint),
            (byte)sizeof(bool),
            (byte)sizeof(DateTime),
            (byte)sizeof(uint/*IPAddress*/),
            (byte)sizeof(Guid),
        };
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static Encoding m_strEncoding = Encoding.GetEncoding(936);
    }

    public unsafe static partial class BinaryFormatter
    {
        public static int SizeBy(Type type)
        {
            if (type == null)
                return 0;
            int i = m_mapTabType.IndexOf(type);
            if (i < 0)
                return 0;
            return m_mapTabSize[i];
        }

        private static byte[] ValueToBinary(Type type, object value, int size)
        {
            if (type == typeof(float))
                return BitConverter.GetBytes(Convert.ToSingle(value));
            if (type == typeof(double))
                return BitConverter.GetBytes(Convert.ToDouble(value));
            if (type == typeof(DateTime))
                value = Convert.ToDateTime(value).Ticks;
            if (type == typeof(ulong))
                value = (long)((ulong)value);
            if (type == typeof(IPAddress))
                value = BitConverter.ToInt32(((IPAddress)value).GetAddressBytes(), 0);
            if (type == typeof(Guid))
            {
                byte[] guid = new byte[sizeof(Guid)];
                fixed (byte* pinned = guid)
                    *(Guid*)pinned = (Guid)value;
                return guid;
            }
            {
                long num = Convert.ToInt64(value);
                byte[] buffer = new byte[size];
                for (int i = 0; i < size; i++)
                    buffer[i] = (byte)((num >> i * 8) & 0xFF);
                return buffer;
            }
        }

        private static void AddLenToStream(BinaryWriter writer, bool single, int len = -1)
        {
            if (single)
                writer.Write((sbyte)len);
            else
                writer.Write((ushort)len);
        }

        private static bool AddStringToStream(Type type, string value, BinaryWriter writer, bool single)
        {
            if (type == typeof(string))
            {
                if (value == null)
                    BinaryFormatter.AddLenToStream(writer, single);
                else
                {
                    byte[] buffer = BinaryFormatter.m_strEncoding.GetBytes(value);
                    BinaryFormatter.AddLenToStream(writer, single, buffer.Length);
                    writer.Write(buffer);
                }
                return true;
            }
            return false;
        }

        private static bool AddListToStream(Type type, IList value, BinaryWriter writer, bool single)
        {
            Type clazz = GetArrayElmentType(type);
            if (clazz != null)
            {
                if (value == null)
                    BinaryFormatter.AddLenToStream(writer, single);
                else
                {
                    BinaryFormatter.AddLenToStream(writer, single, value.Count);
                    if (type == typeof(byte[]))
                        writer.Write(value as byte[]);
                    else
                        for (int i = 0; i < value.Count; i++)
                        {
                            object key = value[i];
                            if (clazz.IsValueType || clazz == typeof(string) || clazz == typeof(IPAddress))
                                BinaryFormatter.AddValueToStream(clazz, key, writer, single);
                            else
                                BinaryFormatter.AddObjectToStream(clazz, key, writer, single);
                        }
                }
                return true;
            }
            return false;
        }

        private static bool AddObjectToStream(Type type, object value, BinaryWriter writer, bool single)
        {
            if (BinaryFormatter.GetArrayElmentType(type) == null)
            {
                if (value == null)
                    writer.Write(true);
                else
                {
                    writer.Write(false);
                    IEnumerable<PropertyInfo> props = type.GetProperties().OrderBy((i) => i.MetadataToken);
                    foreach (PropertyInfo prop in props.OrderBy((i) => i.MetadataToken))
                    {
                        if (Attributes.GetAttribute<IgnoreAttribute>(prop) != null)
                            continue;
                        object obj = prop.GetValue(value, null);
                        BinaryFormatter.AddValueToStream(prop.PropertyType, obj, writer, single);
                    }
                }
                return true;
            }
            return false;
        }

        private static bool AddValueToStream(Type clazz, object value, BinaryWriter writer, bool single)
        {
            if (clazz.IsEnum)
                clazz = clazz.GetEnumUnderlyingType();
            int size = BinaryFormatter.SizeBy(clazz);
            if (size > 0)
            {
                writer.Write(BinaryFormatter.ValueToBinary(clazz, value, size));
                return true;
            }
            else
            {
                if (BinaryFormatter.AddStringToStream(clazz, value as string, writer, single))
                    return true;
                if (BinaryFormatter.AddListToStream(clazz, value as IList, writer, single))
                    return true;
                if (BinaryFormatter.AddObjectToStream(clazz, value, writer, single))
                    return true;
            }
            return false;
        }
    }

    public static partial class BinaryFormatter
    {
        private static Type GetArrayElmentType(Type array)
        {
            if (array.IsArray)
                return array.GetElementType();
            if (array.IsGenericType && (typeof(IList<>).GUID == array.GUID || typeof(List<>).GUID == array.GUID))
                return array.GetGenericArguments()[0];
            return null;
        }

        private static IList CreateArray(Type array, Type element, int len)
        {
            if (array.IsArray)
                return Array.CreateInstance(element, len);
            if (array.IsGenericType && (typeof(IList<>).GUID == array.GUID || typeof(List<>).GUID == array.GUID))
            {
                Type clazz = typeof(List<>).MakeGenericType(element);
                ConstructorInfo ctor = clazz.GetConstructor(new Type[] { typeof(int) });
                return (IList)ctor.Invoke(new object[] { len });
            }
            return null;
        }
    }

    public static partial class BinaryFormatter
    {
        public static Encoding DefaultEncoding { get => m_strEncoding; set => m_strEncoding = value; }

        private static object LongToValue(Type type, long value)
        {
            Type clazz = type;
            if (clazz.IsEnum)
                type = clazz.GetEnumUnderlyingType();
            object result = null;
            if (type == typeof(int))
                result = unchecked((int)value);
            else if (type == typeof(uint))
                result = unchecked((uint)value);
            else if (type == typeof(long))
                result = value;
            else if (type == typeof(ulong))
                result = (ulong)value;
            else if (type == typeof(bool))
                result = value > 0;
            else if (type == typeof(byte))
                result = unchecked((byte)value);
            else if (type == typeof(sbyte))
                result = unchecked((sbyte)value);
            else if (type == typeof(short))
                result = unchecked((short)value);
            else if (type == typeof(ushort))
                result = unchecked((ushort)value);
            else if (type == typeof(double))
                result = BitConverter.Int64BitsToDouble((long)value);
            else if (type == typeof(float))
                result = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
            else if (type == typeof(DateTime))
                result = new DateTime(Convert.ToInt64(value));
            else if (type == typeof(IPAddress))
                result = new IPAddress(value);
            if (clazz.IsEnum)
                result = Enum.ToObject(clazz, result);
            return result;
        }

        private static int Len(BinaryReader reader, bool single)
        {
            return single ? reader.ReadByte() : reader.ReadUInt16();
        }

        private static bool Null(int len, bool single)
        {
            if (single)
                return len == byte.MaxValue;
            return len == ushort.MaxValue;
        }

        private static unsafe string GetStringFromStream(BinaryReader reader, bool single)
        {
            int len = BinaryFormatter.Len(reader, single);
            if (BinaryFormatter.Null(len, single))
            {
                return null;
            }
            if (len <= 0)
            {
                return string.Empty;
            }
            return BinaryFormatter.m_strEncoding.GetString(reader.ReadBytes(len));
        }

        private static object GetListFromStream(Type array, BinaryReader reader, bool single)
        {
            Type element = BinaryFormatter.GetArrayElmentType(array);
            if (element == null)
                return null;
            int len = BinaryFormatter.Len(reader, single);
            if (BinaryFormatter.Null(len, single))
                return null;
            if (array == typeof(byte[]))
                return reader.ReadBytes(len);
            IList buffer = BinaryFormatter.CreateArray(array, element, len);
            for (int i = 0; i < len; i++)
            {
                object value = null;
                if (element.IsValueType || element == typeof(string) || element == typeof(IPAddress))
                    value = BinaryFormatter.GetValueFromStream(element, reader, single);
                else
                    value = BinaryFormatter.GetObjectFromStream(element, reader, single);
                if (buffer.IsFixedSize)
                    buffer[i] = value;
                else
                    buffer.Add(value);
            }
            return buffer;
        }

        private unsafe static object BinaryToValue(Type type, byte[] buffer, int size)
        {
            if (size < 0)
                size = BinaryFormatter.SizeBy(type);
            if (type == typeof(Guid))
                fixed (byte* pinned = buffer)
                    return *(Guid*)pinned;
            long value = 0;
            for (int i = 0; i < size; i++)
                value |= ((long)buffer[i] & 0xFF) << (i * 8);
            return BinaryFormatter.LongToValue(type, value);
        }

        private static object GetObjectFromStream(Type type, BinaryReader reader, bool single)
        {
            if (reader.ReadBoolean())
                return null;
            object key = Activator.CreateInstance(type);
            IEnumerable<PropertyInfo> props = type.GetProperties();
            foreach (PropertyInfo prop in props.OrderBy((i) => i.MetadataToken))
            {
                if (Attributes.GetAttribute<IgnoreAttribute>(prop) != null)
                    continue;
                object value = BinaryFormatter.GetValueFromStream(prop.PropertyType, reader, single);
                prop.SetValue(key, value, null);
            }
            return key;
        }

        private static object GetValueFromStream(Type clazz, BinaryReader reader, bool single)
        {
            Type origin = clazz;
            if (clazz.IsEnum)
                clazz = clazz.GetEnumUnderlyingType();
            int size = BinaryFormatter.SizeBy(clazz);
            if (size > 0)
                return BinaryFormatter.BinaryToValue(origin, reader.ReadBytes(size), size);
            else
            {
                if (clazz == typeof(string))
                    return BinaryFormatter.GetStringFromStream(reader, single);
                else if (BinaryFormatter.GetArrayElmentType(clazz) != null)
                    return BinaryFormatter.GetListFromStream(clazz, reader, single);
                else
                    return BinaryFormatter.GetObjectFromStream(clazz, reader, single);
            }
        }
    }

    public static partial class BinaryFormatter
    {
        public static void Serialize(object graph, Stream s, bool single)
        {
            if (graph == null || s == null)
                throw new ArgumentNullException();
            if (!s.CanWrite)
                throw new ArgumentNullException();
            using (BinaryWriter writer = new BinaryWriter(s))
            {
                BinaryFormatter.AddValueToStream(graph.GetType(), graph, writer, single);
            }
        }

        public static byte[] Serialize(object graph, bool single)
        {
            if (graph == null)
                throw new ArgumentNullException("graph");
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter.Serialize(graph, ms, single);
                return ms.ToArray();
            }
        }

        public static object Deserialize(byte[] buffer, Type type, bool single)
        {
            if (buffer == null || type == null)
                throw new ArgumentNullException();
            using (Stream ms = new MemoryStream(buffer))
            {
                return BinaryFormatter.Deserialize(ms, type, single);
            }
        }

        public static T Deserialize<T>(byte[] buffer, bool single)
        {
            object value = BinaryFormatter.Deserialize(buffer, typeof(T), single);
            if (value == null)
                return default(T);
            return (T)value;
        }

        public static object Deserialize(Stream s, Type type, bool single)
        {
            if (s == null || type == null)
                throw new ArgumentNullException();
            if (!s.CanRead)
                throw new ArgumentException();
            using (BinaryReader reader = new BinaryReader(s))
            {
                return BinaryFormatter.GetObjectFromStream(type, reader, single);
            }
        }

        public static T Deserialize<T>(Stream s, bool single)
        {
            object obj = BinaryFormatter.Deserialize(s, typeof(T), single);
            if (obj != null)
                return (T)obj;
            return default(T);
        }

        [Obsolete]
        public static int SizeOf(object graph, bool single)
        {
            if (graph == null)
                return 0;
            return BinaryFormatter.Serialize(graph, single).Length;
        }
    }
#pragma warning restore 0675, 0618
}