namespace GVMServer.Cryptography.Final
{
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// 一个用于安全的SHA1转换引擎服务
    /// </summary>
    public static class SHA1
    {
        /// <summary>
        /// 将 8 位无符号整数的数组转换为 SHA1 哈希编码的等效字符串表示形式。
        /// </summary>
        /// <param name="buffer">一个 8 位无符号整数数组。</param>
        /// <returns>buffer 的内容的字符串表示形式，以 SHA1 表示。</returns>
        public static string ToSHA1String(byte[] buffer)
        {
            if (buffer == null && buffer.Length <= 0)
            {
                return string.Empty;
            }
            using (System.Security.Cryptography.SHA1 md5 = System.Security.Cryptography.SHA1.Create())
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
        /// 将有效的字符串按照编码转换为 SHA1 哈希编码的等效字符串表示形式。
        /// </summary>
        /// <param name="value">欲被转换的字符串</param>
        /// <param name="encoding">欲被转换字符串所使用的编码</param>
        /// <returns></returns>
        public static string ToSHA1String(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return SHA1.ToSHA1String(encoding.GetBytes(value));
        }
        /// <summary>
        /// 将有效的字符串按照UTF-8编码转换为 SHA1 哈希编码的等效字符串表示形式。
        /// </summary>
        /// <param name="value">欲被转换的字符串</param>
        /// <returns></returns>
        public static string ToSHA1String(string value)
        {
            return SHA1.ToSHA1String(value, Encoding.UTF8);
        }
    }
}
