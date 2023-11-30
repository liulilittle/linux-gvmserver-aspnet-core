namespace GVMServer.Ns.Event
{
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Functional;

    public class LoadAllPlanningConfigurationEvent : IEvent
    {
        public NsPlanningConfiguration Ns { get; set; }
    }
}
