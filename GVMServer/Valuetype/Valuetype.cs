namespace GVMServer.Valuetype
{
    using GVMServer.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Text;

    public static partial class Valuetype
    {
        private static IList<Type> m_numberType = new Type[] {
                                                      typeof(long),
                                                      typeof(ulong),
                                                      typeof(byte),
                                                      typeof(sbyte),
                                                      typeof(short),
                                                      typeof(ushort),
                                                      typeof(char),
                                                      typeof(decimal),
                                                      typeof(int),
                                                      typeof(uint),
        };

        private static IList<Type> m_floatType = new Type[] {
                  typeof(double),
                  typeof(float)
        };

        public static bool IsNumberType(Type type)
        {
            if (type == null)
            {
                return false;
            }
            for (int i = 0; i < m_numberType.Count; i++)
            {
                if (m_numberType[i] == type)
                {
                    return true;
                }
            }
            return false;
        }

        public static Type GetUnderlyingType(Type type)
        {
            if (type != null && type.IsEnum)
            {
                return Enum.GetUnderlyingType(type);
            }
            return type;
        }

        public static bool IsBasicType(Type type)
        {
            return type == typeof(IPAddress) ||
                type == typeof(Guid) || 
                Valuetype.IsDateTime(type) ||
                Valuetype.IsNumberType(type) || 
                Metatype.IsString(type) || 
                Valuetype.IsFloatType(type);
        }

        public static bool IsNumberType(object o)
        {
            if (o == null)
            {
                return false;
            }
            if (o is Valuetype)
            {
                return Valuetype.IsNumberType(o.GetType());
            }
            return false;
        }

        public static bool IsFloatType(Type type)
        {
            if (type == null)
            {
                return false;
            }
            for (int i = 0; i < m_floatType.Count; i++)
            {
                if (m_floatType[i] == type)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsFloatType(object o)
        {
            if (o == null)
            {
                return false;
            }
            if (o is Valuetype)
            {
                return Valuetype.IsFloatType(o.GetType());
            }
            return false;
        }

        public static bool IsDateTime(object o)
        {
            if (o == null)
            {
                return false;
            }
            if (o is Valuetype)
            {
                return Valuetype.IsFloatType(o.GetType());
            }
            return false;
        }

        public static bool IsDateTime(Type type)
        {
            if (type == null)
            {
                return false;
            }
            return (type == typeof(DateTime));
        }

        public static bool IsULong(Type type)
        {
            return type == typeof(ulong);
        }

        public static bool IsIPAddress(Type type)
        {
            return type == typeof(IPAddress);
        }

        public static bool IsValueType(Type type)
        {
            if (type == null)
            {
                return false;
            }
            if (type == typeof(Valuetype))
            {
                return true;
            }
            return type.IsSubclassOf(typeof(Valuetype));
        }
    }
    public static partial class Valuetype
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IList<Type> m_type = new Type[] { typeof(double), typeof(float), typeof(long), typeof(ulong), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(char), typeof(int), typeof(uint), typeof(bool), typeof(DateTime), typeof(IPAddress) };
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IList<byte> m_size = new byte[] { 8, 4, 8, 8, 1, 1, 2, 2, 2, 4, 4, 1, 8, 4 };
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static Encoding m_enc = Encoding.Default;

        public static int SizeBy(Type type)
        {
            int i = m_type.IndexOf(type);
            if (i < 0)
            {
                return 0;
            }
            return m_size[i];
        }

        public static object LongTo(Type type, long value)
        {
            unchecked
            {
                if (type == typeof(int))
                    return (int)value;
                if (type == typeof(uint))
                    return Convert.ToUInt32(value);
                if (type == typeof(long))
                    return value;
                if (type == typeof(ulong))
                    return (ulong)value;
                if (type == typeof(bool))
                    return value > 0;
                if (type == typeof(byte))
                    return Convert.ToByte(value);
                if (type == typeof(sbyte))
                    return Convert.ToSByte(value);
                if (type == typeof(short))
                    return Convert.ToInt16(value);
                if (type == typeof(ushort))
                    return Convert.ToUInt16(value);
                if (type == typeof(double))
                    return BitConverter.Int64BitsToDouble(value);
                if (type == typeof(float))
                    return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
                if (type == typeof(DateTime))
                    return new DateTime(value);
                if (type == typeof(IPAddress))
                    return new IPAddress(value);
                return null;
            }
        }

        public static long ToLong(Type type, byte[] buffer, int size)
        {
            long value = 0;
            unchecked
            {
                for (int i = 0; i < size; i++)
                {
                    if (i >= buffer.Length)
                        break;
                    value |= ((long)buffer[i] & 0xFF) << (i * 8);
                }
            }
            return value;
        }

        public static byte[] BinaryBy(Type type, long value, int size)
        {
            if (size < 0)
                size = Valuetype.SizeBy(type);
            byte[] buffer = new byte[size];
            unchecked
            {
                for (int i = 0; i < size; i++)
                    buffer[i] = (byte)((value >> i * 8) & 0xFF);
            }
            return buffer;
        }

        public static byte[] BinaryBy(Type type, double value)
        {
            if (type == typeof(float))
                return BitConverter.GetBytes(Convert.ToSingle(value));
            if (type == typeof(double))
                return BitConverter.GetBytes(Convert.ToDouble(value));
            return new byte[0];
        }

        public static byte[] BinaryBy(ulong value)
        {
            return Valuetype.BinaryBy(typeof(long), Convert.ToInt64(value), sizeof(ulong));
        }
    }
}
