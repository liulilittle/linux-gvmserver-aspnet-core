namespace GVMServer.Ns.EventHandler
{
    using System;
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Event;
    using GVMServer.Planning;

    public class ReadAllPlanningConfigurationEventHandler : IEventHandler<ReadAllPlanningConfigurationEvent>
    {
        public virtual void Handle(ReadAllPlanningConfigurationEvent e, Action<object> callback)
        {
            PlaningConfiguration.ReadAll();
            EventBus.Current.Publish(new LoadAllPlanningConfigurationToNodeClusterEvent()
            {
                Ns = e.Ns
            });
        }
    }
}
