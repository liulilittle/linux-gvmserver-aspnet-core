namespace GVMServer.Ns.EventHandler
{
    using System;
    using GVMServer.DDD.Events;
    using GVMServer.Ns.Event;
    using GVMServer.Planning;
    using Microsoft.Extensions.Configuration;

    public class PreparationPlanningConfigurationEventHandler : IEventHandler<PreparationPlanningConfigurationEvent>
    {
        public virtual void Handle(PreparationPlanningConfigurationEvent e, Action<object> callback)
        {
            IConfiguration configuration = e.Ns.GetConfiguration();
            if (configuration != null)
            {
                string[] includeDirectories = configuration.GetSection("IncludeDirectories").Get<string[]>();
                string[] preprocessorStdafx = configuration.GetSection("PreprocessorStdafx").Get<string[]>();

                PlaningConfiguration.AddStdafx(preprocessorStdafx);
                PlaningConfiguration.AddInclude(includeDirectories);
            }
        }
    }
}
