namespace GVMServer.W3Xiyou.Docking.Api.Request
{
    public class FeedbackAccountRequest
    {
        public byte AppCategory { get; set; }

        public string AccountId { get; set; }

        public string RoleId { get; set; }

        public string ReportContent { get; set; }
    }
}
