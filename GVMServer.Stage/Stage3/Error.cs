namespace GVMServer.Stage.Stage3
{
    public enum Error : ushort
    {
        Error_Success,
        Error_PendingAuthentication,
        Error_TheObjectHasBeenFreed,
        Error_UnableToFindThisServer,
        Error_SvrAreaNoItIsIllegal,
        Error_NoLinkTypeIsHostedOnTheStage,
        Error_TheInternalServerIsNotOnline,
        Error_LimitTheNumberOfInternalServerClients,
        Error_TheLinkNoCouldNotBeAssigned,
        Error_UnableToAllocSocketVectorObject,
        Error_UnableToAllocServerObject,
        Error_UnableTryGetValueSocketVectorObject,
        Error_UnableToPostEstablishLinkToInternalServer,
        Error_SeriousServerInternalError,
        Error_UnableToPushAllAcceptClient,
    };
}
