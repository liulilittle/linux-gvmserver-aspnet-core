namespace GVMServer.W3Xiyou.Docking.Api.Request
{
    using GVMServer.Serialization.Ssx;

    public class LoginAccountRequest
    {
        public string UserToken { get; set; }

        public uint ClientIP { get; set; }

        public string ClientInfo { get; set; }

        public static string MeasureCode()
        {
            return CppStaticBinaryFormatter.CreateFormatterText(typeof(LoginAccountRequest));
        }
    }
}
