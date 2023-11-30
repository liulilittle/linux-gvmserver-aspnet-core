namespace GVMServer.Csn.Handler
{
    using System.Collections.Generic;
    using GVMServer.Csn.Protobuf;
    using GVMServer.Csn.Ranking;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net.Mvh;

    [SocketMvhHandler(ApplicationType = new[] { ApplicationType.ApplicationType_GameServer, ApplicationType.ApplicationType_CrossServer },
        CommandId = Commands.Commands_MatchRankingGamePlayers, SerializationMode = ObjectSerializationMode.Protobuf)]
    public class MatchRankingGamePlayersHandler : ISocketMvhHandler
    {
        public virtual int AckTimeout => 1000;

        public virtual int AckRetransmission => 3;

        public virtual void ProcessRequest(SocketMvhContext context)
        {
            MatchRankingGamePlayersResponse response = new MatchRankingGamePlayersResponse()
            {
                Code = Error.Error_Success
            };
            do
            {
                MatchRankingGamePlayersRequest request = context.Request.Read<MatchRankingGamePlayersRequest>();
                if (request == null)
                {
                    response.Code = Error.Error_TheRequestModelsObjectCouldNotBeReadIn;
                    break;
                }

                LeaderboardObject<RankingMember> leaderboard = ServiceObjectContainer.Get<Leaderboard>().Get(request.Platform, request.LeaderboardType);
                if (leaderboard == null) // 找到此标志类型的排行榜对象实例
                {
                    response.Code = Error.Error_CannotFindLeaderboardObjectsThatMatchTheProvidedsCriteria;
                    break;
                }

                PatternMatchesObject<RankingMember> min = null;
                PatternMatchesObject<RankingMember> max = null;
                if (request.Min != null)
                {
                    min = new PatternMatchesObject<RankingMember>()
                    {
                        AchievementScore = request.Min.AchievementScore,
                        AchievementTime = request.Min.AchievementTime
                    };
                }

                if (request.Max != null)
                {
                    max = new PatternMatchesObject<RankingMember>()
                    {
                        AchievementScore = request.Max.AchievementScore,
                        AchievementTime = request.Max.AchievementTime
                    };
                }

                response.Code = leaderboard.Sharding.Matches(min, max, request.MaxNumberOfMatches, out IEnumerable<RankingMember> s);
                response.Matches = s ?? PatternMatchesObject<RankingMember>.EmptyMatches;
            } while (false);
            context.Response.Write(response, AckTimeout, AckRetransmission);
        }
    }
}
