namespace GVMServer.Ns.Functional
{
    using System;
    using System.Collections.Generic;
    using GVMServer.DDD.Events;
    using GVMServer.DDD.Service;
    using GVMServer.Ns.Collections;
    using GVMServer.Ns.Event;
    using GVMServer.Planning;
    using Microsoft.Extensions.Configuration;

    public class NsPlanningConfiguration : IServiceBase
    {
        public NsPlanningConfiguration(BaseApplication application)
        {
            this.Application = application ?? throw new ArgumentNullException(nameof(application));
            EventBus.Current.Publish(new PreparationPlanningConfigurationEvent()
            {
                Ns = this
            });
        }

        public BaseApplication Application { get; }

        public IConfiguration GetConfiguration()
        {
            IConfiguration configuration = this.Application.GetConfiguration();
            if (configuration == null)
                return null;
            return configuration.GetSection("PlanningConfiguration");
        }

        public virtual Dictionary<TValue> GetAllFromNodeCluster<TValue>()
        {
            return new Dictionary<TValue>(new BucketSet<TValue>());
        }

        public virtual void LoadAllFromLocalHost()
        {
            EventBus.Current.Publish(new LoadAllPlanningConfigurationEvent()
            {
                Ns = this
            });
        }

        public virtual void ReadAllFromLocalHost()
        {
            EventBus.Current.Publish(new ReadAllPlanningConfigurationEvent()
            {
                Ns = this
            });
        }

        public virtual IList<TValue> GetAllFromLocalHost<TValue>() where TValue : class
        {
            return PlaningConfiguration.GetAll<TValue>();
        }

        public virtual void ClearAllFromLocalHost()
        {
            EventBus.Current.Publish(new ReleaseAllPlanningConfigurationEvent()
            {
                Ns = this
            });
        }
    }
}
