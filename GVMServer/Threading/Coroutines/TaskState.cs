namespace GVMServer.Threading.Coroutines
{
    public enum TaskState : int
    {
        kRunning,
        kAborted,
        kSuspended,
        kStopped,
        kSleeping
    }
}
