namespace GVMServer.Ns.Event
{
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Functional;

    public class ReadAllPlanningConfigurationEvent : IEvent
    {
        public NsPlanningConfiguration Ns { get; set; }
    }
}
