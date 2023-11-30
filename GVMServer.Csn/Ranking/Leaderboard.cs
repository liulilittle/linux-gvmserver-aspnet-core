namespace GVMServer.Csn.Ranking
{
    using GVMServer.Csn.Ranking.Fundamental;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;

    public class Leaderboard : Leaderboard<RankingMember>, IServiceBase
    {
        public Leaderboard(BaseApplication application) : base(application)
        {

        }
    }
}
