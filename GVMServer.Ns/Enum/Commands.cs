namespace GVMServer.Ns.Enum
{
    public enum Commands : ushort
    {
        Commands_Authentication,
        Commands_LinkHeartbeat,
        Commands_Transitroute,
        Commands_Echo,
        Commands_AcceptSocket,
        Commands_AbortSocket,
        Commands_AcceptAllSocket,
        Commands_RoutingMessage,
        Commands_QueryAllAvailableServers,
        Commands_MatchAvavailableServer,
        Commands_RankingMemberInformationBulkUpdate,
        Commands_MatchRankingGamePlayers,
        Commands_AllServerAverageRankingUpdate,
        Commands_ClosingActivityNotification,
        Commands_PreopeningActivityRequest,
    }
}
