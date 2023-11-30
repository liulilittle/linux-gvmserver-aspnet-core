namespace GVMServer.Cryptography
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class Crypto
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static HashSet<string> m_ciphers = new HashSet<string>
        {
            "aes-128-cfb",
            "aes-128-ofb",
            "aes-256-cfb",
            "aes-256-ofb",
            "aes-192-cfb",
            "aes-192-ofb",
            "rc4-sha1",
            "rc4-md5",
        };

        public static IEnumerable<string> Ciphers
        {
            get
            {
                return m_ciphers;
            }
        }

        public static bool Contains(string cipher)
        {
            if (string.IsNullOrEmpty(cipher))
                return false;
            return m_ciphers.Contains(cipher);
        }

        public static ICryptography Create(string cipher, string key)
        {
            if (string.IsNullOrEmpty(cipher) || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException();
            }
            switch (cipher)
            {
                case "aes-128-cfb":
                    return new AES128CFB(key);
                case "aes-128-ofb":
                    return new AES128OFB(key);
                case "aes-256-cfb":
                    return new AES256CFB(key);
                case "aes-256-ofb":
                    return new AES256OFB(key);
                case "aes-192-cfb":
                    return new AES192CFB(key);
                case "aes-192-ofb":
                    return new AES192OFB(key);
                case "rc4-sha1":
                    return new RC4SHA1(key);
                case "rc4-md5":
                    return new RC4MD5(key);
            }
            throw new ArgumentException("cipher");
        }
    }
}
