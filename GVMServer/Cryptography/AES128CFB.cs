namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;

    public class AES128CFB : AES // AES-128-CFB
    {
        public AES128CFB(string key) : base(key, 256, 128, 128, PaddingMode.PKCS7, CipherMode.CFB)
        {

        }
    }
}
