namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;

    public class AES256OFB : AES // AES-256-OFB
    {
        public AES256OFB(string key) : base(key, 256, 256, 128, PaddingMode.PKCS7, CipherMode.OFB)
        {

        }
    }
}
