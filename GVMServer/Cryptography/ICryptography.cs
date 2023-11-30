namespace GVMServer.Cryptography
{
    public interface ICryptography
    {
        byte[] Encrypt(byte[] value, int ofs, int len);

        byte[] Decrypt(byte[] value, int ofs, int len);
    }
}
