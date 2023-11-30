namespace GVMServer.Web.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.InteropServices;

    [TypeLibType(TypeLibTypeFlags.FHidden | TypeLibTypeFlags.FRestricted)]
    [ClassInterface(ClassInterfaceType.None)]
    public class ServerAddInfo
    {
        public int sid { get; set; }

        public int aid { get; set; }

        public int chatsvr_port { get; set; }

        public int gamesvr_port { get; set; }

        public string chatsvr_address { get; set; }

        public string gamesvr_address { get; set; }

        [DataType(DataType.Text)]
        public string platform { get; set; }

        [DataType(DataType.Text)]
        public string paid { get; set; }

        [DataType(DataType.Text)]
        public string group_name { get; set; }

        [DataType(DataType.Text)]
        public string server_name { get; set; }
    }
}
