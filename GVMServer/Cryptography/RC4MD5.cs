namespace GVMServer.Cryptography
{
    using GVMServer.Cryptography.Final;

    public class RC4MD5 : RC4
    {
        public RC4MD5(string key) : base(key, SBox(MD5.ToMD5String(key)))
        {

        }
    }
}
