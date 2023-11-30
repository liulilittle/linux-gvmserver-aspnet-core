namespace GVMServer.Csn.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using GVMServer.Csn.Modules;
    using GVMServer.Csn.Planning;
    using GVMServer.Csn.Protobuf.Activities;
    using GVMServer.Csn.Ranking;
    using GVMServer.DDD.Service;
    using GVMServer.Linq;
    using GVMServer.Ns;
    using GVMServer.Ns.Collections;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Mvh;
    using GVMServer.Utilities;
    using Monitor = GVMServer.Ns.Collections.Monitor;
    using WorkingActivitiesSet = GVMServer.Ns.Collections.SortedSet<GVMServer.Ns.Collections.Value<uint>>;

    public class ActivitiesService : IServiceBase
    {
        private volatile bool m_disposed = false;
        private readonly Thread m_poWorkThread = null;

        public virtual Dictionary<ActivityConfiguration> ActivitiesTable { get; }

        public ActivitiesService()
        {
            this.m_poWorkThread = new Thread(() =>
            {
                DateTime dtLastTickAlwaysTime = DateTime.Now;
                while (!this.m_disposed)
                {
                    DateTime dtCurrentTime = DateTime.Now;
                    TimeSpan tsElapsedTime = dtCurrentTime - dtLastTickAlwaysTime;
                    if (tsElapsedTime.TotalSeconds >= 1)
                    {
                        dtLastTickAlwaysTime = dtCurrentTime;
                        this.OnActivitiesTickAlways();
                    }
                    Thread.Sleep(1);
                }
            })
            { IsBackground = true, Priority = ThreadPriority.Lowest };
            this.m_poWorkThread.Start();
            this.ActivitiesTable = ServiceObjectContainer.Get<NsPlanningConfiguration>().GetAllFromNodeCluster<ActivityConfiguration>();
        }

        public virtual ActivityState GetActivityState(ActivityConfiguration configuration)
        {
            if (configuration == null)
            {
                return ActivityState.ActivityState_Unknow;
            }

            long currentTime = DateTime.Now.ToTimespan10();
            if (currentTime < configuration.PreopeningTime)
            {
                return ActivityState.ActivityState_Unknow;
            }

            if (currentTime < configuration.OpenTime)
            {
                return ActivityState.ActivityState_PreopeningActivity;
            }

            long closeTime = configuration.OpenTime + configuration.DurationTime;
            if (0 == configuration.DurationTime || currentTime < closeTime)
            {
                return ActivityState.ActivityState_OpenActivity | ActivityState.ActivityState_PreopeningActivity;
            }

            return ActivityState.ActivityState_ClosingActivity;
        }

        protected virtual bool PredicateHandlingAll(Predicate<ActivityConfiguration> predicate)
        {
            if (predicate == null)
            {
                return false;
            }

            var enumerables = this.ActivitiesTable.Bucket.GetAll(out Error error);
            if (error != Error.Error_Success)
            {
                return false;
            }

            if (enumerables == null)
            {
                return true;
            }

            foreach (var kv in enumerables)
            {
                ActivityConfiguration configuration = kv.Value;
                if (configuration == null)
                {
                    continue;
                }

                if (!predicate(configuration))
                {
                    break;
                }
            }
            return true;
        }

        protected virtual void OnActivitiesTickAlways()
        {
            this.PredicateHandlingAll((configuration) =>
            {
                try
                {
                    ActivityState activityState = configuration.GetActivityState();
                    if (0 != (activityState & ActivityState.ActivityState_Unknow))
                    {
                        return true;
                    }
                    else
                    {
                        configuration.Platform = configuration.Platform ?? string.Empty;
                        this.Synchronize(configuration.ActivityId, () =>
                        {
                            if (0 != (activityState & ActivityState.ActivityState_PreopeningActivity))
                            {
                                return this.ProcessPreopeningActivity(configuration);
                            }
                            else if (0 != (activityState & ActivityState.ActivityState_OpenActivity))
                            {
                                return this.ProcessOpenActivity(configuration);
                            }
                            else if (0 != (activityState & ActivityState.ActivityState_ClosingActivity))
                            {
                                return this.ProcessCloseActivity(configuration);
                            }
                            return Error.Error_Success;
                        }, false);
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        public Error CloseActivity(uint activityId)
        {
            ActivityConfiguration configuration = this.ActivitiesTable.Bucket.Get(new ActivityConfiguration() { ActivityId = activityId }, out Error error);
            if (error != Error.Error_Success)
            {
                return error;
            }
            return this.CloseActivity(configuration);
        }

        public virtual Error CloseActivity(ActivityConfiguration configuration)
        {
            if (configuration == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }
            return this.Synchronize(configuration.ActivityId, () => this.ProcessCloseActivity(configuration));
        }

        protected virtual Error ProcessOpenActivity(ActivityConfiguration configuration)
        {
            return Error.Error_Success;
        }

        protected virtual ulong NewActivityInstanceId()
        {
            ServerApplication application = ServiceObjectContainer.Get<ServerApplication>();
            return Convert.ToUInt64(application.GenerateNewId());
        }

        protected virtual Error Synchronize(uint activityId, Func<Error> critical, bool localTaken = true, int timeout = 3)
        {
            Monitor monitor = this.GetMonitor(activityId);
            if (!localTaken)
            {
                if (monitor.LocalTaken(out Error error))
                {
                    return Error.Error_MonitorObjectIsRemotingNodeClusterLocking;
                }
                else if (error != Error.Error_Success)
                {
                    return error;
                }
            }
            return monitor.Synchronize(critical, timeout);
        }

        protected enum MonitorCategory : long
        {
            ActivityConfigurationLockId = NodeEnvironment.ActivityConfigurationLockId,
            PreopeningActivityAsyncLockId = NodeEnvironment.PreopeningActivityAsyncLockId,
        }

        protected virtual Monitor GetMonitor(long activityId, MonitorCategory category = MonitorCategory.ActivityConfigurationLockId)
        {
            long d = unchecked((long)category);
            return new Monitor(d + activityId);
        }

        protected virtual Error MatchesActivitySeveralGrouping(ActivityConfiguration configuration, out List<ActivitySeveralGrouping> severals)
        {
            severals = new List<ActivitySeveralGrouping>();
            if (configuration == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }

            ServerLeaderboard leaderboards = ServiceObjectContainer.Get<ServerLeaderboard>();
            if (leaderboards == null)
            {
                return Error.Error_UnableToFetchServerLeaderboardTheObjectInstacne;
            }

            int severalInGroups = unchecked((int)configuration.Parameters.Index(0));
            if (severalInGroups < 2)
            {
                severalInGroups = 2;
            }

            int rankingType = unchecked((int)configuration.Parameters.Index(1));
            int maxMatching = unchecked((int)configuration.Parameters.Index(2));
            long minScore = configuration.Parameters.Index(3);
            long maxScore = configuration.Parameters.Index(4);

            Error error = leaderboards.MatchesOffPlatform(configuration.Platform, rankingType, minScore, maxScore, maxMatching, out IEnumerable<ServerRankingMember> s);
            if (error != Error.Error_Success)
            {
                return error;
            }
            else
            {
                do
                {
                    ActivitySeveralGrouping grouping = default(ActivitySeveralGrouping);
                    if (s == null)
                    {
                        break;
                    }

                    Netif netif = ServiceObjectContainer.Get<Netif>();
                    foreach (ServerRankingMember ranking in s)
                    {
                        if (ranking == null)
                        {
                            continue;
                        }

                        ISocket socket = netif.GetSocket(ApplicationType.ApplicationType_GameServer, ranking.Platform, ranking.ServerNo);
                        if (socket == null || 0 != socket.Credentials.BattleSuit)
                        {
                            continue;
                        }

                        bool allowAdding = false;
                        if (grouping == null)
                        {
                            grouping = new ActivitySeveralGrouping()
                            {
                                ServerCamps = new List<ActivityServerCamping>(),
                                Platform = configuration.Platform,
                                ActivityId = configuration.ActivityId,
                                PlatformCrosserver = configuration.Platform,
                                InstanceId = this.NewActivityInstanceId(),
                            };
                            allowAdding |= true;
                        }

                        grouping.ServerCamps.Add(new ActivityServerCamping()
                        {
                            Platform = ranking.Platform,
                            ServerNo = ranking.ServerNo,
                            InstanceId = this.NewActivityInstanceId()
                        });
                        if (allowAdding)
                        {
                            severals.Add(grouping);
                        }

                        if (grouping.ServerCamps.Count >= severalInGroups)
                        {
                            grouping = null;
                        }
                    }
                } while (false);
            }
            return error;
        }

        protected virtual Error MatchesActivityWrokingBattleSuit(ActivityConfiguration configuration, List<ActivitySeveralGrouping> severals)
        {
            if (configuration == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }

            // 无法匹配服务器时这个活动则不被允许向外开放
            if (!severals.Any())
            {
                return Error.Error_ThisEventIsNotAllowedToOpenWhenTheBattleServerCannotBeMatched;
            }

            // 匹配活动可用跨服游戏服务器节点(暂为平均分配)
            var channels = ServiceObjectContainer.Get<Netif>().GetAllSockets(configuration.Platform, ApplicationType.ApplicationType_CrossServer);
            if (!channels.Any())
            {
                return Error.Error_ThereAreCurrentlyNoCombatClothingNodesThatAreOnlineInTheCluster;
            }

            // 为每个活动服务器阵营组分配一个跨服节点
            int index = 0;
            for (int i = 0; i < severals.Count; i++)
            {
                foreach (var channel in channels)
                {
                    if (index >= severals.Count)
                    {
                        break;
                    }

                    if (channel == null || !channel.Available)
                    {
                        continue;
                    }

                    ActivitySeveralGrouping serveral = severals[index];
                    if (serveral == null)
                    {
                        continue;
                    }

                    index++;
                    serveral.PlatformCrosserver = channel.Credentials.PlatformName;
                    serveral.CrossserverNo = channel.Credentials.ServerNo;
                }
            }
            return Error.Error_Success;
        }

        protected virtual WorkingActivitiesSet GetWorkingActivitiesSet()
        {
            return new WorkingActivitiesSet(new BucketSet<Value<uint>>(new SortedSetAccessor(Csn.NodeEnvironment.WorkingActivitiesSet)));
        }

        protected virtual Dictionary<ActivitySeveralGrouping> GetActivitySeveralGroupingTable(uint activityId)
        {
            return new Dictionary<ActivitySeveralGrouping>(new BucketSet<ActivitySeveralGrouping>(new SortedSetAccessor(Csn.NodeEnvironment.ActivitySeveralGroupingTable + activityId)));
        }

        protected virtual Error ProcessPreopeningActivity(ActivityConfiguration configuration)
        {
            if (configuration == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }

            // 当前处于预开启活动异步工作状态
            Monitor workingAsyncMonitor = this.GetMonitor(configuration.ActivityId, MonitorCategory.PreopeningActivityAsyncLockId);
            if (workingAsyncMonitor.LocalTaken(out Error error) || error != Error.Error_Success)
            {
                return error;
            }

            WorkingActivitiesSet workingActivitiesSet = this.GetWorkingActivitiesSet();
            if (workingActivitiesSet.Contains(configuration.ActivityId)) // 正在处于工作中的活动
            {
                return Error.Error_Success;
            }

            // 匹配活动需求的服务器阵营
            error = this.MatchesActivitySeveralGrouping(configuration, out List<ActivitySeveralGrouping> severals);
            if (error != Error.Error_Success)
            {
                return error;
            }

            // 匹配活动工作的战斗服阵营
            error = this.MatchesActivityWrokingBattleSuit(configuration, severals);
            if (error != Error.Error_Success)
            {
                return error;
            }

            // 申请异步的活动预开启监视分布式对象（最大死锁时间约定20秒）
            workingAsyncMonitor.Enter(20);

            // 现在尝试分配当前开启活动
            Dictionary<ActivitySeveralGrouping> groupings = this.GetActivitySeveralGroupingTable(configuration.ActivityId);
            error = groupings.Bucket.Clear();
            if (error != Error.Error_Success)
            {
                return error;
            }

            // 开始往返确认分配的跨服游戏服务器节点是否可用机器
            this.RoundTripPreopeningActivity(configuration, severals, workingAsyncMonitor, workingActivitiesSet);
            return error;
        }

        protected virtual void RoundTripPreopeningActivity(ActivityConfiguration configuration, List<ActivitySeveralGrouping> severals, Monitor workingAsyncMonitor, WorkingActivitiesSet workingActivitiesSet)
        {
            // 开始往返确认分配的跨服游戏服务器节点是否可用机器
            var counts = 0;
            void errorf(ActivitySeveralGrouping m)
            {
                if (Interlocked.Increment(ref counts) >= severals.Count)
                {
                    // 释放异步的活动预开启监视分布式对象
                    workingAsyncMonitor.Exit();
                }
            }
            var channels = ServiceObjectContainer.Get<Netif>().GetAllSockets(configuration.Platform, ApplicationType.ApplicationType_CrossServer);
            for (int i = 0; i < severals.Count; i++)
            {
                ActivitySeveralGrouping serverl = severals[i];
                if (serverl == null)
                {
                    continue;
                }
                if (!this.RoundTripPreopeningActivity(configuration, serverl, channels.GetEnumerator(), () =>
                {
                    // 添加活动Id到正在工作的活动散列集合之中
                    workingActivitiesSet.Add(configuration.ActivityId);

                    // 释放异步的活动预开启监视分布式对象
                    workingAsyncMonitor.Exit();
                }, errorf))
                {
                    errorf(serverl);
                }
            }

        }

        private bool RoundTripPreopeningActivity(ActivityConfiguration configuration, ActivitySeveralGrouping serverl, IEnumerator<ISocket> sockets, Action success, Action<ActivitySeveralGrouping> error)
        {
            if (serverl == null || configuration == null)
            {
                return false;
            }

            ISocket current = sockets.Current;
            if (current != null)
            {
                serverl.CrossserverNo = current.Credentials.ServerNo;
                serverl.Platform = current.Credentials.PlatformName;
            }

            PreopeningActivityRequest request = new PreopeningActivityRequest
            {
                Activity = configuration,
                SeveralGrouping = serverl,
                PlatformName = configuration.Platform,
                State = (int)this.GetActivityState(configuration)
            };
            return request.InitiateAsync(serverl.GetSocketCrosserver(), (context, message) =>
            {
                bool retraining = false;
                if (context.RetransmissionState == SocketMvhRetransmissionState.AckSuccess)
                {
                    PreopeningActivityResponse response = message.Deserialize<PreopeningActivityResponse>(ObjectSerializationMode.Protobuf);
                    if (response == null || 0 != response.Code) // 无法要求跨服游戏服务器预开启活动
                    {
                        retraining = true;
                    }
                    else
                    {
                        Dictionary<ActivitySeveralGrouping> groupings = this.GetActivitySeveralGroupingTable(configuration.ActivityId);
                        if (groupings.Bucket.Add(serverl) != Error.Error_Success)
                        {
                            retraining = true;
                        }
                        else
                        {
                            if (success != null)
                            {
                                success();
                            }
                            this.BoardcastToAllGameServer(configuration, serverl);
                        }
                    }
                }
                else if (context.RetransmissionState == SocketMvhRetransmissionState.AckTimeout)
                {
                    retraining = true;
                }
                if (retraining)
                {
                    if (!sockets.MoveNext())
                    {
                        if (error != null)
                        {
                            error(serverl);
                        }
                    }
                    else
                    {
                        this.RoundTripPreopeningActivity(configuration, serverl, sockets, success, error);
                    }
                }
            });
        }

        protected virtual Error BoardcastToAllGameServer(ActivityConfiguration configuration, ActivitySeveralGrouping serverl, RetransmissionEvent ackEvent = default(RetransmissionEvent))
        {
            if (configuration == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }
            Netif netif = ServiceObjectContainer.Get<Netif>();
            do
            {
                IEnumerable<ISocket> sockets = serverl.ServerCamps.Conversion(m =>
                    netif.GetSocket(ApplicationType.ApplicationType_GameServer, m.Platform, m.ServerNo));
                if (sockets.IsNullOrEmpty())
                {
                    break;
                }
                foreach (ISocket socket in sockets)
                {
                    PreopeningActivityRequest request = new PreopeningActivityRequest
                    {
                        Activity = configuration,
                        SeveralGrouping = serverl,
                        PlatformName = configuration.Platform,
                        State = (int)this.GetActivityState(configuration)
                    };
                    request.InitiateAsync(socket, ackEvent);
                }
            } while (false);
            return Error.Error_Success;
        }

        protected virtual Error ProcessCloseActivity(ActivityConfiguration configuration)
        {
            if (configuration == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }

            // 从正在工作的活动集合之中删除这个活动
            WorkingActivitiesSet workingActivitiesSet = this.GetWorkingActivitiesSet();
            workingActivitiesSet.Remove(configuration.ActivityId);

            // 释放活动预开启时持有的服务器阵营数据
            Dictionary<ActivitySeveralGrouping> groupings = this.GetActivitySeveralGroupingTable(configuration.ActivityId);
            groupings.Clear();

            return Error.Error_Success;
        }
    }
}
