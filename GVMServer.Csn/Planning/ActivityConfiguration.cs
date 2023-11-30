namespace GVMServer.Csn.Planning
{
    using GVMServer.Csn.Services;
    using GVMServer.DDD.Service;
    using GVMServer.Ns.Collections;
    using GVMServer.Planning.PlanningXml;
    using ProtoBuf;

    public enum ActivityState
    {
        ActivityState_Unknow = 0,
        ActivityState_PreopeningActivity = 1,
        ActivityState_OpenActivity = 2,
        ActivityState_ClosingActivity = 4,
    }

    [XmlFileName("crossserveractivity_data.xml", "CrossserverActivityConfiguration.xml")]
    [ProtoContract]
    public class ActivityConfiguration : IKey
    {
        [ProtoMember(1)]
        public virtual uint ActivityId { get; set; }
        /// <summary>
        /// 活动预解放时间
        /// </summary>
        [ProtoMember(2)]
        public uint PreopeningTime { get; set; }
        /// <summary>
        /// 活动解放时间
        /// </summary>
        [ProtoMember(3)]
        public uint OpenTime { get; set; }
        /// <summary>
        /// 活动持续时间
        /// </summary>
        [ProtoMember(4)]
        public uint DurationTime { get; set; }
        /// <summary>
        /// 活动类型
        /// </summary>
        [ProtoMember(5)]
        public uint ActivityType { get; set; }
        /// <summary>
        /// 平台编号
        /// </summary>
        [ProtoMember(6)]
        public string Platform { get; set; }
        /// <summary>
        /// 活动参数
        /// </summary>
        [ProtoMember(7)]
        public long[] Parameters { get; set; }
        /// <summary>
        /// 设备场景地图Id
        /// </summary>
        [ProtoMember(8)]
        public uint SceneMapId { get; set; }
        /// <summary>
        /// 活动状态
        /// </summary>
        public virtual ActivityState GetActivityState()
        {
            return ServiceObjectContainer.Get<ActivitiesService>().GetActivityState(this);
        }
        /// <summary>
        /// 获取唯一Id
        /// </summary>
        /// <returns></returns>
        public virtual string GetKey()
        {
            return this.ActivityId.ToString();
        }
    }
}
