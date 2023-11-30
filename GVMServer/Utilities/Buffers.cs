namespace GVMServer.Utilities
{
    using System;
    using System.Net;

    public unsafe static class Buffers
    {
        public static readonly byte[] EmptryBuffer = new byte[0];

        public static int memcmp(IPAddress x, IPAddress y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }

            if (x.AddressFamily != y.AddressFamily)
            {
                return x.AddressFamily > y.AddressFamily ? 1 : -1;
            }

            byte[] bx= x.GetAddressBytes();
            byte[] by = y.GetAddressBytes();
            return memcmp(bx, 0, by, 0, Math.Max(bx.Length, by.Length));
        }

        public static int memcmp(byte[] src, int startIndex, byte[] dest, int destIndex, int count)
        {
            if (startIndex < 0 || destIndex < 0)
            {
                return 0;
            }

            if (src == null && dest == null)
            {
                return 0;
            }
            else if (src == null && dest != null)
            {
                return -1;
            }
            else if (src != null && dest == null)
            {
                return 1;
            }

            if (count <= 0)
            {
                return 0;
            }

            int slen = Math.Min(src.Length - startIndex, count);
            int dlen = Math.Min(dest.Length - destIndex, count);
            if (slen != dlen)
            {
                return slen > dlen ? 1 : -1;
            }

            if (slen < 0 && dlen >= 0)
            {
                return -1;
            }
            else if (slen >= 0 && dlen < 0)
            {
                return 1;
            }

            fixed (byte* psrc = &src[startIndex])
            {
                fixed (byte* pdest = &dest[destIndex])
                {
                    return memcmp(psrc, pdest, slen);
                }
            }
        }

        public static int memcmp(byte* src, byte* dest, int count)
        {
            if (src == null && dest == null)
            {
                return 0;
            }

            if (count <= 0)
            {
                return 0;
            }

            for (int i = 0; i < count; i++)
            {
                byte sb = *src++;
                byte db = *dest++;
                if (sb == db)
                {
                    continue;
                }
                else
                {
                    return sb > db ? 1 : -1;
                }
            }
            return 0;
        }

        public static bool IsZeroMemory(byte[] buffer)
        {
            int len = buffer.Length;
            for (int i = 0; i < len; i++)
            {
                if (buffer[i] != 0)
                    return false;
            }
            return true;
        }

        public static void* memcpy(byte[] src, byte[] dest, int count)
        {
            fixed (byte* pinned = dest)
            {
                return memcpy(src, pinned, count);
            }
        }

        public static void* memcpy(byte* src, byte[] dest, int count)
        {
            return memcpy(src, dest, 0, count);
        }

        public static void* memcpy(byte* src, byte[] dest, int ofs, int count)
        {
            fixed (byte* pinned = &dest[ofs])
            {
                return memcpy(src, pinned, count);
            }
        }

        public static void* memcpy(byte[] src, int ofs, void* dest, int count)
        {
            fixed (byte* pinned = &src[ofs])
            {
                return memcpy(pinned, dest, count);
            }
        }

        public static void* memcpy(byte[] src, void* dest, int count)
        {
            return memcpy(src, 0, dest, count);
        }

        public static void* memcpy(void* src, void* dest, int count)
        {
            if (dest == null || src == null)
            {
                return null;
            }
            if (dest == src && count <= 0)
            {
                return dest;
            }
            Buffers.BlockCopy((byte*)src, 0, (byte*)dest, count);
            return dest;
        }

        public static void BlockCopy(byte* src, int ofs, byte[] dest)
        {
            BlockCopy(src, ofs, dest, dest.Length);
        }

        public static void BlockCopy(byte* src, int ofs, byte[] dest, int len)
        {
            fixed (byte* pinned = dest)
            {
                BlockCopy(src, ofs, pinned, len);
            }
        }

        public static void BlockCopy(byte* src, int ofs, byte* dest, int len)
        {
            if (src == null || dest == null)
            {
                return;
            }
            src = &src[ofs];
            int count = (len / sizeof(long));
            byte* end = &src[len];
            long* x = (long*)src;
            long* y = (long*)dest;
            for (int i = 0; i < count; i++)
            {
                *y++ = *x++;
            }
            src = (byte*)x;
            dest = (byte*)y;
            for (; src != end;)
            {
                *dest++ = *src++;
            }
        }

        public static void BlockCopy(byte* sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int length)
        {
            fixed (byte* pinned = &destinationArray[destinationIndex])
            {
                BlockCopy(&sourceArray[sourceIndex], 0, pinned, length);
            }
        }

        public static void BlockCopy(byte[] src, int ofs, byte[] desc, int len)
        {
            fixed (byte* x = &src[ofs])
            {
                fixed (byte* y = desc)
                {
                    BlockCopy(x, ofs, y, len);
                }
            }
        }

        public static void BlockCopy(byte[] src, byte[] desc, int len)
        {
            BlockCopy(src, 0, desc, len);
        }

        public static void BlockCopy(byte[] src, int ofs, byte[] desc)
        {
            BlockCopy(src, ofs, desc, desc.Length);
        }
    }
}
