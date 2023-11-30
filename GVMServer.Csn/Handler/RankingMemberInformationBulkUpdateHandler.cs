namespace GVMServer.Csn.Handler
{
    using GVMServer.Csn.Protobuf;
    using GVMServer.Csn.Ranking;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net.Mvh;

    [SocketMvhHandler(ApplicationType = new[] { ApplicationType.ApplicationType_GameServer },
       CommandId = Commands.Commands_RankingMemberInformationBulkUpdate, SerializationMode = ObjectSerializationMode.Protobuf)]
    public class RankingMemberInformationBulkUpdateHandler : ISocketMvhHandler
    {
        public virtual int AckTimeout => 1000;

        public virtual int AckRetransmission => 3;

        public virtual void ProcessRequest(SocketMvhContext context)
        {
            RankingMemberInformationBulkUpdateResponse response = new RankingMemberInformationBulkUpdateResponse
            {
                Code = Error.Error_Success
            };
            do
            {
                RankingMemberInformationBulkUpdateRequest request = context.Request.Read<RankingMemberInformationBulkUpdateRequest>();
                if (request == null)
                {
                    response.Code = Error.Error_TheRequestModelsObjectCouldNotBeReadIn;
                    break;
                }

                var leaderboards = ServiceObjectContainer.Get<Leaderboard>().GetAll(request.Platform, request.LeaderboardType);
                if (leaderboards.Length <= 0) // 找到此标志类型的排行榜对象实例
                {
                    response.Code = Error.Error_CannotFindLeaderboardObjectsThatMatchTheProvidedsCriteria;
                    break;
                }

                foreach (var leaderboard in leaderboards)
                {
                    if (leaderboard == null)
                    {
                        continue;
                    }
                    response.Events += leaderboard.AddBulkUpdate(request.Members);
                }
            } while (false);
            context.Response.Write(response, AckTimeout, AckRetransmission);
        }
    }
}
