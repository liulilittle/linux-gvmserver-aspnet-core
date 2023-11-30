namespace GVMServer.Converter
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    public static unsafe partial class BitConverterr
    {
        public static int ToInt32(ref byte* p)
        {
            return (int)ToInt64(ref p, sizeof(int));
        }

        public static uint ToUInt32(ref byte* p)
        {
            return (uint)ToInt64(ref p, sizeof(uint));
        }

        public static ushort ToUInt16(ref byte* p)
        {
            return (ushort)ToInt64(ref p, sizeof(ushort));
        }

        public static long ToInt64(ref byte* p)
        {
            return ToInt64(ref p, sizeof(long));
        }

        public static short ToInt16(ref byte* p)
        {
            return (short)ToInt64(ref p, sizeof(short));
        }

        public static ulong ToUInt64(ref byte* p)
        {
            return (ulong)ToInt64(ref p, sizeof(ulong));
        }

        public static byte ToByte(ref byte* p)
        {
            return *p++;
        }

        public static sbyte ToSByte(ref byte* p)
        {
            return (sbyte)*p++;
        }

        private static long ToInt64(ref byte* p, int size)
        {
#pragma warning disable CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
            long num = 0;
            byte* x = (byte*)&num;
            for (int i = size - 1, j = 0; i >= 0; i--, j++)
            {
                x[j] = p[i];
            }
            p += size;
            return num;
#pragma warning restore CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
        }

        public static DateTime ToDateTime(ref byte* p)
        {
            return new DateTime(ToInt64(ref p, sizeof(long)));
        }

        public static double ToDouble(ref byte* p)
        {
            long num = ToInt64(ref p, sizeof(long));
            return BitConverter.Int64BitsToDouble(num);
        }

        public static float ToSingle(ref byte* p)
        {
            byte[] buf = new byte[4];
            for (int i = 3; i >= 0; i--)
                buf[i] = *p++;
            return BitConverter.ToSingle(buf, 0);
        }

        public static IPAddress ToIPAddress(ref byte* p)
        {
            byte[] buf = new byte[4];
            for (int i = 3; i >= 0; i--)
                buf[i] = *p++;
            return new IPAddress(buf);
        }

        public static bool ToBoolean(ref byte* buf)
        {
            return *buf++ != 0;
        }
    }

    public static unsafe partial class BitConverterr
    {
        public static byte[] GetBytes(long num)
        {
            return GetBytes(num, sizeof(long));
        }

        public static byte[] GetBytes(ulong num)
        {
            return GetBytes((long)num, sizeof(ulong));
        }

        public static byte[] GetBytes(uint num)
        {
            return GetBytes(num, sizeof(uint));
        }

        public static byte[] GetBytes(int num)
        {
            return GetBytes(num, sizeof(int));
        }

        public static byte[] GetBytes(short num)
        {
            return GetBytes(num, sizeof(short));
        }

        public static byte[] GetBytes(ushort num)
        {
            return GetBytes(num, sizeof(ushort));
        }

        public static byte[] GetBytes(char ch)
        {
            return GetBytes(ch, sizeof(char));
        }

        public static byte[] GetBytes(sbyte num)
        {
            return GetBytes(num, sizeof(sbyte));
        }

        public static byte[] GetBytes(bool num)
        {
            return GetBytes(num ? 1 : 0, sizeof(bool));
        }

        public static byte[] GetBytes(byte num)
        {
            return GetBytes(num, sizeof(byte));
        }

        public static byte[] GetBytes(double num)
        {
            long value = BitConverter.DoubleToInt64Bits(num);
            return GetBytes(value, sizeof(long));
        }

        public static byte[] GetBytes(float num)
        {
            return GetBytes(*(int*)&num, sizeof(float));
        }

        public static byte[] GetBytes(DateTime datetime)
        {
            return GetBytes(datetime.Ticks, sizeof(long));
        }

        public static byte[] GetBytes(IPAddress address)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            if (address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException();
            return GetBytes(address.Address, sizeof(int));
#pragma warning restore CS0618 // 类型或成员已过时
        }

        private static byte[] GetBytes(long value, int size)
        {
            byte[] buf = new byte[size];
            byte* y = (byte*)&value;
            fixed(byte* x = buf)
            {
                for (int i = size - 1, j = 0; i >= 0; i--, j++)
                    x[j] = y[i];
            }
            return buf;
        }
    }
}
