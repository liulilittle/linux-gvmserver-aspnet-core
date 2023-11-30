namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;
    using System.Text;

    public class AES : ICryptography
    {
        private RijndaelManaged m_aes;
        private byte[] m_key;
        private byte[] m_iv;

        public AES(string key, int blockSize, int keySize, int feedbackSize, PaddingMode padding, CipherMode mode)
        {
            m_aes = new RijndaelManaged();
            m_aes.BlockSize = blockSize;
            m_aes.KeySize = keySize;
            m_aes.FeedbackSize = feedbackSize;
            m_aes.Padding = padding;
            m_aes.Mode = mode;
            using (SHA256 sha256 = SHA256.Create())
            {
                m_key = sha256.ComputeHash(Encoding.ASCII.GetBytes(key));
            }
            m_iv = IV(m_key);
            m_aes.IV = m_iv;
        }

        public byte[] Encrypt(byte[] buffer, int ofs, int len)
        {
            using (ICryptoTransform encryptor = m_aes.CreateEncryptor(m_key, m_iv))
            {
                return encryptor.TransformFinalBlock(buffer, ofs, len);
            }
        }

        public byte[] Decrypt(byte[] buffer, int ofs, int len)
        {
            using (ICryptoTransform decryptor = m_aes.CreateDecryptor(m_key, m_iv))
            {
                return decryptor.TransformFinalBlock(buffer, ofs, len);
            }
        }

        protected virtual byte[] IV(byte[] buffer)
        {
            byte[] num = new byte[buffer.Length];
            for (int i = 0, j = 0; i < buffer.Length; i++, j++)
            {
                byte n = (byte)(buffer[i] >> 4);
                num[j] = (byte)(n <= 9 ? '0' + n : 'a' + n - 10);
            }
            return num;
        }
    }
}
