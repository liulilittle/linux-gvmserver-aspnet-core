namespace GVMServer.Cryptography.Final
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// 一个用于安全的MD5转换引擎服务
    /// </summary>
    public static class MD5
    {
        /// <summary>
        /// 将 8 位无符号整数的数组转换为 MD5 哈希编码的等效字符串表示形式。
        /// </summary>
        /// <param name="buffer">一个 8 位无符号整数数组。</param>
        /// <returns>buffer 的内容的字符串表示形式，以 MD5 表示。</returns>
        public static string ToMD5String(byte[] buffer)
        {
            if (buffer == null && buffer.Length <= 0)
            {
                return string.Empty;
            }
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                buffer = md5.ComputeHash(buffer);
                if (buffer == null || buffer.Length <= 0)
                {
                    return string.Empty;
                }
                string message = string.Empty;
                for (int i = 0; i < buffer.Length; i++)
                {
                    message += buffer[i].ToString("X2");
                }
                return message;
            }
        }
        /// <summary>
        /// 将有效的字符串按照编码转换为 MD5 哈希编码的等效字符串表示形式。
        /// </summary>
        /// <param name="value">欲被转换的字符串</param>
        /// <param name="encoding">欲被转换字符串所使用的编码</param>
        /// <returns></returns>
        public static string ToMD5String(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return MD5.ToMD5String(encoding.GetBytes(value));
        }
        /// <summary>
        /// 将有效的字符串按照UTF-8编码转换为 MD5 哈希编码的等效字符串表示形式。
        /// </summary>
        /// <param name="value">欲被转换的字符串</param>
        /// <returns></returns>
        public static string ToMD5String(string value)
        {
            return MD5.ToMD5String(value, Encoding.UTF8);
        }

        /// <summary>
        /// MD5 16位加密 加密后密码为小写
        /// </summary>
        /// <param name="convertString"></param>
        /// <returns></returns>
        public static string ToMD5String16(string convertString, Encoding encoding)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            string t2 = BitConverter.ToString(md5.ComputeHash(encoding.GetBytes(convertString)), 4, 8);
            t2 = t2.Replace("-", "");
            t2 = t2.ToLower();

            return t2;
        }
    }
}
