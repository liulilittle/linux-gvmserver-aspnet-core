namespace GVMServer.Ns.EventHandler
{
    using System;
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Event;
    using GVMServer.Planning;

    public class ReleaseAllPlanningConfigurationEventHandler : IEventHandler<ReleaseAllPlanningConfigurationEvent>
    {
        public virtual void Handle(ReleaseAllPlanningConfigurationEvent e, Action<object> callback)
        {
            PlaningConfiguration.Clear();
            PlaningConfiguration.Release();
        }
    }
}
