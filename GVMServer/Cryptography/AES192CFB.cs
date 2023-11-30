namespace GVMServer.Cryptography
{
    using System.Security.Cryptography;

    public class AES192CFB : AES // AES-192-CFB
    {
        public AES192CFB(string key) : base(key, 256, 192, 128, PaddingMode.PKCS7, CipherMode.CFB)
        {

        }
    }
}
