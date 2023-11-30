namespace GVMServer.W3Xiyou.Docking.Api.Request
{
    using GVMServer.Serialization.Ssx;

    public class CreateOrderRequest
    {
        public byte AppCategory { get; set; }

        public string UserId { get; set; }

        public int ProductId { get; set; }

        public string ProduceName { get; set; }

        public string ProduceDesc { get; set; }

        public int BuyNum { get; set; }

        public int Money { get; set; }

        public long RoleId { get; set; }

        public string RoleName { get; set; }

        public int RoleLevel { get; set; }

        public string Extension { get; set; }

        public string PidClientFlags { get; set; }

        public uint ClientIP { get; set; }

        public string ClientInfo { get; set; }

        public static string MeasureCode()
        {
            return CppStaticBinaryFormatter.CreateFormatterText(typeof(CreateOrderRequest));
        }
    };
}
