namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;

    public class AES128OFB : AES // AES-128-OFB
    {
        public AES128OFB(string key) : base(key, 256, 128, 128, PaddingMode.PKCS7, CipherMode.OFB)
        {

        }
    }
}
