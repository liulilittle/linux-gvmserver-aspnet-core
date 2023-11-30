namespace GVMServer.Cryptography
{
    using GVMServer.Cryptography.Final;

    public class RC4SHA1 : RC4
    {
        public RC4SHA1(string key) : base(key, SBox(SHA1.ToSHA1String(key)))
        {

        }
    }
}
