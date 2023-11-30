namespace GVMServer.Csn.EventHandler
{
    using System;
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Event;
    using GVMServer.Planning;

    public class LoadAllPlanningConfigurationEventHandler : IEventHandler<LoadAllPlanningConfigurationEvent>
    {
        public virtual void Handle(LoadAllPlanningConfigurationEvent e, Action<object> callback)
        {
            PlaningConfiguration.LoadAll(typeof(CsnApplication).Assembly);
        }
    }
}
