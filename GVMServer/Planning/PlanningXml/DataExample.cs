namespace GVMServer.Planning.PlanningXml
{
    using System;

    public static class DataExample
    {
        public class RewardItem
        {
            public UInt16 Type { set; get; }

            public UInt32 ID { set; get; }

            public UInt16 QualityId { set; get; }

            public UInt32 Num { set; get; }
        }

        [XmlFileName("achievement_data.xml", "AchievementConfig.xml")]
        public class AchievementConfig
        {
            public UInt16 AchievementId { set; get; }

            public UInt16 TitleId { set; get; }

            public UInt16 AchievementType { set; get; }

            public UInt16 NextAchievementID { set; get; }

            public UInt32[] AchievementCondEvent { set; get; }

            public Int64 AchievementParam { set; get; }

            public RewardItem[] RewardItem { set; get; }

            public UInt32[] AchievementEvent { set; get; }

            public Int64 Gold { set; get; }

            public Int64 Exp { set; get; }

            public Int64 Drop { set; get; }

            public Int64 MountBless { set; get; }

            public Int64 WingBless { set; get; }
        }

        // 用例
        public static void ReadAll()
        {
            PlaningConfiguration.AddInclude(
                @"F:\dd\scheme\DataBin\Server",
                @"F:\dd\scheme\resxml\res_server");
            PlaningConfiguration.AddStdafx(
                @"F:\dd\scheme\resxml\common\common.xml", 
                @"F:\dd\scheme\resxml\common\keywords.xml", 
                @"F:\dd\scheme\resxml\common\rescommon.xml");
            PlaningConfiguration.LoadAll(typeof(DataExample).Assembly);
            var configurations = PlaningConfiguration.GetAll<AchievementConfig>();
            PlaningConfiguration.Clear();
        }
    }
}
