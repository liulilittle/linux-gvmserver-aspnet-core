namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;

    public class AES192OFB : AES // AES-192-OFB
    {
        public AES192OFB(string key) : base(key, 256, 192, 128, PaddingMode.PKCS7, CipherMode.OFB)
        {

        }
    }
}
