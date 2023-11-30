namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;

    public class AES256CFB : AES // AES-256-CFB
    {
        public AES256CFB(string key) : base(key, 256, 256, 128, PaddingMode.PKCS7, CipherMode.CFB)
        {

        }
    }
}