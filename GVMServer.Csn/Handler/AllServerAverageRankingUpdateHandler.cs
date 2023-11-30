namespace GVMServer.Csn.Handler
{
    using System;
    using GVMServer.Csn.Protobuf;
    using GVMServer.Csn.Ranking;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Utilities;
    using GVMServer.Ns.Net.Mvh;
    using GVMServer.Ns.Integers;
    using System.Runtime.InteropServices;

    [SocketMvhHandler(ApplicationType = new[] { ApplicationType.ApplicationType_GameServer }, 
        CommandId = Commands.Commands_AllServerAverageRankingUpdate, SerializationMode = ObjectSerializationMode.Protobuf)]
    public unsafe class AllServerAverageRankingUpdateHandler : ISocketMvhHandler
    {
        public virtual int AckTimeout => 1000;

        public virtual int AckRetransmission => 3;

        public virtual void ProcessRequest(SocketMvhContext context)
        {
            AllServerAverageRankingUpdateResponse response = new AllServerAverageRankingUpdateResponse
            {
                Code = Error.Error_Success
            };
            do
            {
                AllServerAverageRankingUpdateRequest request = context.Request.Read<AllServerAverageRankingUpdateRequest>();
                if (request == null)
                {
                    response.Code = Error.Error_TheRequestModelsObjectCouldNotBeReadIn;
                    break;
                }

                if (string.IsNullOrEmpty(request.Platform))
                {
                    response.Code = Error.Error_PlatformTextIsNullOrEmptyThisNotAllow;
                    break;
                }

                if (request.ServerNo <= 0)
                {
                    response.Code = Error.Error_GameServerApplicationNotAllowInputServerNoLessOrEqualsZero;
                    break;
                }

                Int128* achievementScore = stackalloc Int128[1]; // 从栈上分配一个变量
                *achievementScore = 0;

                if (request.Values == null)
                {
                    break;
                }

                foreach (var value in request.Values)
                {
                    if (value == null)
                    {
                        break;
                    }

                    var leaderboards = ServiceObjectContainer.Get<ServerLeaderboard>().GetAll(request.Platform, value.LeaderboardType);
                    if (leaderboards.Length <= 0) // 找到此标志类型的排行榜对象实例
                    {
                        response.Code = Error.Error_CannotFindLeaderboardObjectsThatMatchTheProvidedsCriteria;
                        break;
                    }

                    *achievementScore = 0;
                    if (value.ScoreValue != null)
                    {
                        int destiantionLength = Math.Min(sizeof(Int128), value.ScoreValue.Length);
                        Marshal.Copy(value.ScoreValue, 0, (IntPtr)achievementScore, destiantionLength);
                    }

                    foreach (var leaderboard in leaderboards)
                    {
                        if (leaderboard == null)
                        {
                            continue;
                        }
                        ServerRankingMember ranking = new ServerRankingMember()
                        {
                            AchievementTime = Convert.ToUInt32(DateTime.Now.ToTimespan10()),
                            MemberNo = Convert.ToUInt32(request.ServerNo),
                            Platform = request.Platform,
                            ServerNo = request.ServerNo,
                            AchievementScore = 0,
                            RankingIndex = 0
                        };
                        ranking.SetAchievementScore(unchecked((decimal)*achievementScore));
                        if (!leaderboard.AddBulkUpdate(ranking))
                        {
                            continue;
                        }
                        response.Events++;
                    }
                }
            } while (false);
            context.Response.Write(response, AckTimeout, AckRetransmission);
        }
    }
}
