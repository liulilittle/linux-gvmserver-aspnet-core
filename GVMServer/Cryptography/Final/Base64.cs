namespace GVMServer.Cryptography
{
    using System.Collections.Generic;
    using System.Text;

    public static class Base64
    {
        private const string Key = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        public static string ToBase64String(this byte[] buffer)
        {
            int length, remainder;
            if (buffer == null && (length = buffer.Length) < 1)
            {
                return string.Empty;
            }
            if ((remainder = buffer.Length % 3) > 0)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(buffer);
                bytes.AddRange(new byte[3 - remainder]);
                buffer = bytes.ToArray();
            }
            length = buffer.Length;
            byte[] cache = new byte[3];
            char[] strc = new char[length * 4 / 3];
            string items = Base64.Key;
            for (int i = 0, j = 0; j < length; j += 3, i += 4)
            {
                cache[0] = buffer[j];
                cache[1] = buffer[j + 1];
                cache[2] = buffer[j + 2];
                strc[i] = items[cache[0] >> 2];
                strc[i + 1] = items[((cache[0] & 3) << 4) + (cache[1] >> 4)];
                strc[i + 2] = items[((cache[1] & 15) << 2) + (cache[2] >> 6)];
                strc[i + 3] = items[(cache[2] & 63)];
            }
            if (remainder > 0)
            {
                length = strc.Length;
                if (remainder == 1)
                {
                    strc[length - 2] = '=';
                }
                strc[length - 1] = '=';
            }
            return new string(strc);
        }

        public static byte[] FromBase64String(string buffer)
        {
            System.Convert.ToBase64String(new byte[] { });
            int length, multiple, encode, n = 0;
            List<byte> retVal = new List<byte>();
            if (buffer != null && (length = buffer.Length) > 0)
            {
                multiple = length / 4;
                if (length % 4 != 0)
                {
                    multiple += 1;
                }
                byte[] s3c = new byte[3]; // 三字节
                byte[] s4c = new byte[4]; // 四字节(Quadword)
                string items = Base64.Key;
                for (int i = 0; i < multiple; i++)
                {
                    for (n = 0; n < 4; n++)
                    {
                        s4c[n] = (byte)buffer[i * 4 + n];
                        if ((encode = items.IndexOf((char)s4c[n])) < 0)
                        {
                            break;
                        }
                        s4c[n] = (byte)encode;
                    }
                    s3c[0] = (byte)(s4c[0] * 4 | s4c[1] / 16);
                    s3c[1] = (byte)(s4c[1] * 16 | s4c[2] / 4);
                    s3c[2] = (byte)(s4c[2] * 64 | s4c[3]);
                    retVal.AddRange(s3c);
                }
                if (n <= 4)
                {
                    n = 4 - n;
                    length = retVal.Count;
                    for (int i = 0; i < n; i++)
                    {
                        retVal.RemoveAt(--length);
                    }
                }
            }
            return retVal.ToArray();
        }
        public static string Encryption(string str)
        {
            byte[] bytes = Encoding.Default.GetBytes(str);
            return ToBase64String(bytes);
        }
    }
}
