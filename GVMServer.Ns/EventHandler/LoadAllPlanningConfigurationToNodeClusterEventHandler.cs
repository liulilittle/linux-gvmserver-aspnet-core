namespace GVMServer.Ns.EventHandler
{
    using System;
    using System.Reflection;
    using GVMServer.DDD.Events;
    using GVMServer.DDD.Service;
    using GVMServer.Ns.Collections;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Event;
    using GVMServer.Ns.Functional;
    using GVMServer.Planning;
    using GVMServer.Linq;
    using GVMServer.Planning.PlanningXml;

    public class LoadAllPlanningConfigurationToNodeClusterEventHandler : IEventHandler<LoadAllPlanningConfigurationToNodeClusterEvent>
    {
        public virtual void Handle(LoadAllPlanningConfigurationToNodeClusterEvent e, Action<object> callback)
        {
            // 现在尝试载入本地配置数据帧到分布式节点之中
            MethodInfo miGetAllPlaningConfiguration = typeof(PlaningConfiguration).GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
            if (miGetAllPlaningConfiguration == null || !miGetAllPlaningConfiguration.IsGenericMethodDefinition || miGetAllPlaningConfiguration.GetGenericArguments().Length > 1)
            {
                throw new RuntimeException(Error.Error_RuntimeError, "A serious runtime system exception, unable to find the GetAll<T> method information of PlanningConfiguration");
            }

            MethodInfo miGetAllFromNodeCluster = typeof(NsPlanningConfiguration).GetMethod("GetAllFromNodeCluster", BindingFlags.Public | BindingFlags.Instance);
            if (miGetAllFromNodeCluster == null || !miGetAllFromNodeCluster.IsGenericMethodDefinition || miGetAllFromNodeCluster.GetGenericArguments().Length > 1)
            {
                throw new RuntimeException(Error.Error_RuntimeError, "A serious runtime system exception, unable to find the GetAll<T> method information of NsPlanningConfiguration");
            }

            NsPlanningConfiguration center = ServiceObjectContainer.Get<NsPlanningConfiguration>();
            foreach (Type planningConfigurationType in PlaningConfiguration.GetAllExportedTypes())
            {
                if (planningConfigurationType == null || !typeof(IKey).IsAssignableFrom(planningConfigurationType))
                {
                    continue;
                }

                XmlFileNameAttribute xml = planningConfigurationType.GetCustomAttributes().FirstOrDefault(i => i is XmlFileNameAttribute) as XmlFileNameAttribute;
                if (xml == null)
                {
                    continue;
                }

                dynamic configurations = miGetAllPlaningConfiguration.MakeGenericMethod(planningConfigurationType).Invoke(null, null);
                if (configurations == null)
                {
                    continue;
                }

                dynamic management = miGetAllFromNodeCluster.MakeGenericMethod(planningConfigurationType).Invoke(center, null);
                if (management == null)
                {
                    continue;
                }

                dynamic bucket = management.Bucket;
                if (bucket == null)
                {
                    continue;
                }

                Error error = bucket.Synchronize(new Action(() =>
                {
                    management.Clear(); // 先彻底清楚现有集群上面托管的对应类型策划配置案例
                    bucket.AddAll(configurations);
                }));
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }

                Console.WriteLine($"A loading all \"{xml.Key.Key}\" configurations into the node cluster");
            }

            // 现在尝试释放本地已经读入到计算节点内存的本地配置
            EventBus.Current.Publish(new ReleaseAllPlanningConfigurationEvent()
            {
                Ns = e.Ns
            });
        }
    }
}
