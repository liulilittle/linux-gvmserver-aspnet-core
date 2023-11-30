namespace GVMServer.Csn.Ranking
{
    using GVMServer.Csn.Ranking.Fundamental;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;
    using Microsoft.Extensions.Configuration;

    public class ServerLeaderboard : Leaderboard<ServerRankingMember>, IServiceBase
    {
        public ServerLeaderboard(BaseApplication application) : base(application)
        {

        }

        protected override IConfiguration GetConfiguration() => this.Application.GetConfiguration().GetSection("Leaderboard").GetSection("Server");
    }
}
