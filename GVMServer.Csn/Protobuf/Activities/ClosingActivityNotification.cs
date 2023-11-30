namespace GVMServer.Csn.Protobuf.Activities
{
    using System.Collections.Generic;
    using GVMServer.Csn.Planning;
    using ProtoBuf;

    [ProtoContract]
    public class ClosingActivityNotification 
    {
        [ProtoContract]
        public class ClosingActivityMember
        {
            /// <summary>
            /// 活动Id
            /// </summary>
            [ProtoMember(1)]
            public virtual ActivityConfiguration Activity { get; set; }
            /// <summary>
            /// 活动状态
            /// </summary>
            [ProtoMember(2)]
            public virtual ActivityState State { get; set; }
        }
        /// <summary>
        /// 活动列表
        /// </summary>
        [ProtoMember(1)]
        public IList<ClosingActivityMember> Activities { get; } = new List<ClosingActivityMember>();
        /// <summary>
        /// 所属平台
        /// </summary>
        [ProtoMember(2)]
        public string PlatformName { get; set; }
    }
}
