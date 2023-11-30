namespace GVMServer.Ns.EventHandler
{
    using System;
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Event;
    using GVMServer.Planning;

    public class LoadAllPlanningConfigurationEventHandler : IEventHandler<LoadAllPlanningConfigurationEvent>
    {
        public virtual void Handle(LoadAllPlanningConfigurationEvent e, Action<object> callback)
        {
            PlaningConfiguration.LoadAll(typeof(IEventHandler<>).Assembly);
            PlaningConfiguration.LoadAll(typeof(LoadAllPlanningConfigurationEvent).Assembly);
        }
    }
}
