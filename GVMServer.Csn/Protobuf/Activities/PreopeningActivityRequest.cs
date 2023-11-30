namespace GVMServer.Csn.Protobuf.Activities
{
    using System.Collections.Generic;
    using GVMServer.Csn.Modules;
    using GVMServer.Csn.Planning;
    using GVMServer.Csn.Ranking;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;
    using GVMServer.Ns.Collections;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Mvh;
    using ProtoBuf;

    [ProtoContract]
    public class ActivitySeveralGrouping : IKey
    {
        /// <summary>
        /// 活动实例Id
        /// </summary>
        [ProtoMember(1)]
        public ulong InstanceId { get; set; }
        /// <summary>
        /// 活动编号
        /// </summary>
        [ProtoMember(2)]
        public uint ActivityId { get; set; }
        /// <summary>
        /// 匹配到游戏平台
        /// </summary>
        [ProtoMember(3)]
        public string Platform { get; set; }
        /// <summary>
        /// 匹配服务器阵营
        /// </summary>
        [ProtoMember(4)]
        public IList<ActivityServerCamping> ServerCamps { get; set; }
        /// <summary>
        /// 所跨服务器编号
        /// </summary>
        [ProtoMember(5)]
        public int CrossserverNo { get; set; }
        /// <summary>
        /// 所跨服务器平台
        /// </summary>
        [ProtoMember(6)]
        public string PlatformCrosserver { get; set; }
        /// <summary>
        /// 获取当前实例的唯一编号
        /// </summary>
        /// <returns></returns>
        public virtual string GetKey()
        {
            return this.InstanceId.ToString();
        }
        /// <summary>
        /// 获取跨服节点的套接字
        /// </summary>
        /// <returns></returns>
        public virtual ISocket GetSocketCrosserver()
        {
            Netif netif = ServiceObjectContainer.Get<Netif>();
            return netif.GetVirtualSocket(ApplicationType.ApplicationType_CrossServer, this.PlatformCrosserver, this.CrossserverNo);
        }
    }

    [ProtoContract]
    public class PreopeningActivityRequest
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
        public virtual int State { get; set; }
        /// <summary>
        /// 活动阵营
        /// </summary>
        [ProtoMember(3)]
        public virtual ActivitySeveralGrouping SeveralGrouping { get; set; }
        /// <summary>
        /// 游戏平台
        /// </summary>
        [ProtoMember(4)]
        public string PlatformName { get; set; }
        /// <summary>
        /// 发起请求
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="ackEvent">确认事件</param>
        /// <returns></returns>
        public virtual bool InitiateAsync(ISocket socket, RetransmissionEvent ackEvent)
        {
            if (socket == null)
            {
                return false;
            }
            Netif netif = ServiceObjectContainer.Get<Netif>();
            return netif.Send(new[] { socket }, Commands.Commands_PreopeningActivityRequest, this, 3000, 5, ackEvent) > 0;
        }
    }
}
