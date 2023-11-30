namespace GVMServer.Ns.Functional
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using GVMServer.Cache;
    using GVMServer.DDD.Service;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Model;
    using GVMServer.Ns.Net.Model;
    using ServiceStack.Redis;

    public class NsLoadbalancing : IServiceBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly NodeSample[] m_aoNodeSampleEmpty = new NodeSample[0];

        private static string MeasureLoadbalanceingSetKey(ApplicationType applicationType)
        {
            string key = "ns.nsloadbalancing.xset." + (int)applicationType;
            return key;
        }

        private static string MeasureSampleKey(Guid nodeid)
        {
            string key = "ns.nsloadbalancing.sample.info." + nodeid.ToString();
            return key;
        }

        private static string MeasureSampleLockKey(Guid nodeid)
        {
            string key = "ns.nsloadbalancing.sample.lock." + nodeid.ToString();
            return key;
        }

        private Error InternalGetRedisClient(string locker, Func<IRedisClient, Error> callback)
        {
            IRedisClient redis = null;
            try
            {
                using (redis = RedisClientManager.GetDefault().GetClient())
                {
                    IDisposable disposable = null;
                    if (!string.IsNullOrEmpty(locker))
                    {
                        try
                        {
                            Error error = CacheAccessor.AcquireLock(redis, locker, out disposable);
                            if (error != Error.Error_Success)
                            {
                                return error;
                            }
                        }
                        catch (Exception)
                        {
                            return Error.Error_TheDistributedCriticalSectionBlockLockCannotBeObtained;
                        }
                    }
                    try
                    {
                        using (disposable)
                        {
                            Error error = callback(redis);
                            if (error != Error.Error_Success)
                            {
                                return error;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return Error.Error_ProblemsOccurredInReleasingTheDistributedCriticalSectionBlockLocks;
                    }
                }
                return Error.Error_Success;
            }
            catch (Exception)
            {
                if (redis != null)
                {
                    try
                    {
                        redis.Dispose();
                    }
                    catch (Exception)
                    {

                    }
                }
                return Error.Error_TheRedisClientCouldNotBeRetrievedFromTheConnectionPoolManager;
            }
        }

        private Error InternalAddOrRemoveSampingTransaction(ref Guid nodeid, params Func<IRedisClient, Error>[] callback)
        {
            return InternalGetRedisClient(MeasureSampleLockKey(nodeid), (redis) =>
            {
                Error error = Error.Error_Success;
                try
                {
                    using (IRedisTransaction transaction = redis.CreateTransaction())
                    {
                        try
                        {
                            for (int i = 0; i < callback.Length; i++)
                            {
                                transaction.QueueCommand((r) => error = callback[i](r));
                                if (error != Error.Error_Success)
                                {
                                    break;
                                }
                            }
                            bool rollback = false;
                            if (error != Error.Error_Success)
                            {
                                rollback = true;
                            }
                            else
                            {
                                try
                                {
                                    transaction.Commit();
                                }
                                catch (Exception)
                                {
                                    rollback = true;
                                }
                            }
                            if (rollback)
                            {
                                try
                                {
                                    transaction.Rollback();
                                    if (error == Error.Error_Success)
                                    {
                                        error = Error.Error_TheTransactionThatCommittedTheRedisCachedDataFailedOnRollback;
                                    }
                                }
                                catch (Exception)
                                {
                                    if (error == Error.Error_Success)
                                    {
                                        error = Error.Error_TheCommitToTheRedisCacheFailedButThereWasAProblemWithTheRollback;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            error = Error.Error_TheQueueCommandLineForATransactionCannotBeAddedToRedis;
                        }
                    }
                }
                catch (Exception)
                {
                    error = Error.Error_TheTransactionInstanceOfRedisCannotBeOpened;
                }
                return error;
            });
        }
        /// <summary>
        /// 添加采样（提供最新的样本保证集群负载均衡与熔断器系统）
        /// </summary>
        /// <returns></returns>
        public virtual Error AddSampling(ApplicationType applicationType, Guid nodeid, LinkHeartbeat request)
        {
            if (request == null)
            {
                return Error.Error_YourInputTheLinkHeartbeatIsNullReferences;
            }

            return InternalAddOrRemoveSampingTransaction(ref nodeid,
                (r) =>
                {
                    try
                    {
                        r.AddItemToSet(MeasureLoadbalanceingSetKey(applicationType), MeasureSampleKey(nodeid));
                    }
                    catch (Exception)
                    {
                        return Error.Error_UnableToAddTheItemToSetTheDistributedCache;
                    }
                    return Error.Error_Success;
                },
                (r) =>
                {
                    try
                    {
                        r.Set(MeasureSampleKey(nodeid), new NodeSample
                        {
                            Nodeid = nodeid,
                            Context = request,
                            ApplicationType = applicationType,
                        });
                    }
                    catch (Exception)
                    {
                        return Error.Error_UnableToSetRedisCacheToMemoryServer;
                    }
                    return Error.Error_Success;
                });
        }
        /// <summary>
        /// 移除采样（当客户端的任何通道都消失时移除与客户端关联采样）
        /// </summary>
        /// <returns></returns>
        public virtual Error RemoveSampling(ApplicationType applicationType, Guid nodeid)
        {
            return InternalAddOrRemoveSampingTransaction(ref nodeid,
                (r) =>
                {
                    try
                    {
                        r.RemoveItemFromSet(MeasureLoadbalanceingSetKey(applicationType), MeasureSampleKey(nodeid));
                    }
                    catch (Exception)
                    {
                        return Error.Error_UnableToRemoveItemFromSetTheDistributedCache;
                    }
                    return Error.Error_Success;
                },
                (r) =>
                {
                    try
                    {
                        r.Remove(MeasureSampleKey(nodeid));
                    }
                    catch (Exception)
                    {
                        return Error.Error_AKeyValuePairCannotBeRemovedFromTheRedisCacheBuffer;
                    }
                    return Error.Error_Success;
                });
        }
        /// <summary>
        /// 获取所有节点Id
        /// </summary>
        /// <returns></returns>
        public virtual Error GetAllNodeid(ApplicationType applicationType, out HashSet<string> sets)
        {
            HashSet<string> out_ = null;
            try
            {
                Error error = InternalGetRedisClient(string.Empty, (r) =>
                {
                    try
                    {
                        out_ = r.GetAllItemsFromSet(MeasureLoadbalanceingSetKey(applicationType));
                        return Error.Error_Success;
                    }
                    catch (Exception)
                    {
                        return Error.Error_UnableToGetAllItemsFromSetTheDistributedCache;
                    }
                });
                if (out_ == null)
                {
                    out_ = new HashSet<string>();
                }
                return Error.Error_Success;
            }
            finally
            {
                sets = out_;
            }
        }
        /// <summary>
        /// 获取所有采样
        /// </summary>
        /// <returns></returns>
        public virtual Error GetAllSampling(IEnumerable<string> nodeid, out IDictionary<string, NodeSample> samples)
        {
            IDictionary<string, NodeSample> out_ = null;
            try
            {
                Error error = Error.Error_Success;
                if (nodeid != null && null != nodeid.FirstOrDefault())
                {
                    error = InternalGetRedisClient(string.Empty, (r) =>
                    {
                        try
                        {
                            out_ = r.GetAll<NodeSample>(nodeid);
                            return Error.Error_Success;
                        }
                        catch (Exception)
                        {
                            return Error.Error_MultipleKeyValuesCannotBeRetrievedFromTheRedisCache;
                        }
                    });
                }
                if (out_ == null)
                {
                    out_ = new Dictionary<string, NodeSample>();
                }
                return error;
            }
            finally
            {
                samples = out_;
            }
        }
        /// <summary>
        /// 查询服务器（负载均衡）
        /// </summary>
        /// <returns></returns>
        public virtual Error Lookup(ApplicationType applicationType, out NodeSample sampling)
        {
            sampling = null;
            // 查询此类型的全部节点编号信息
            Error error = GetAllNodeid(applicationType, out HashSet<string> nodeid);
            if (error != Error.Error_Success)
            {
                return error;
            }

            // 查询此类型的全部节点采样信息
            error = GetAllSampling(nodeid, out IDictionary<string, NodeSample> samples);
            if (error != Error.Error_Success)
            {
                return error;
            }

            NodeSample[] aoMinSample = new NodeSample[]
            {
                null, // 负载平衡级别（一）【CPU负载最低、物理内存负载都低的服务器节点】
                null, // 负载平衡级别（二）【CPU负载最低、物理内存低于KEEP_PHYSICAL_MEMORY_PERCENTAGE的值】
                null, // 负载平衡级别（三）【CPU负载最低、物理内存高于KEEP_PHYSICAL_MEMORY_PERCENTAGE的值，但是可用内存大于KEEP_PHYSICAL_MEMORY_SIZE的值】
                null, // 负载平衡级别（四）【CPU负载最低、I/O负载小于MAX_NIC_AVAILABLE_BANDWIDTH_PERCENTAGE的值】
                null, // 负载平衡级别（五）【CPU负载最低、I/O负载大于MAX_NIC_AVAILABLE_BANDWIDTH_PERCENTAGE的值】
                null, // 负载平衡级别（六）【各方面负载都非常恶劣；全靠熔断器维持节点继续工作的级别；理论上此级别必须要进行报警增加新的节点】
            };
            void fMinSampleInline(int category, NodeSample sample)
            {
                // 求最小CPU压力的服务器节点
                NodeSample poReferSample = aoMinSample[category];
                if (poReferSample == null || poReferSample.Context.CPULOAD < sample.Context.CPULOAD)
                {
                    aoMinSample[category] = sample;
                }
            }
            foreach (NodeSample sample in samples.Values)
            {
                if (sample == null)
                {
                    continue;
                }

                fMinSampleInline(category: 5, sample: sample);

                // 必须为操作系统保留1G的物理内存（为内核提供一定的内存容量载荷）
                if (sample.Context.AvailablePhysicalMemory <= KEEP_PHYSICAL_MEMORY_SIZE)
                {
                    continue;
                }

                // 必须为节点保证系统网卡计的可用带宽百分比（网卡速度达到极限丢包率会直线提升）
                double perSecondInputAndOutputBandwidth = ((double)(sample.Context.PerSecondBytesReceived + sample.Context.PerSecondBytesSent)
                    / sample.Context.NicMaxBandwidthSpeeds) * 100.00d;
                if (perSecondInputAndOutputBandwidth > MAX_NIC_AVAILABLE_BANDWIDTH_PERCENTAGE)
                {
                    fMinSampleInline(category: 4, sample: sample);
                    continue;
                }

                fMinSampleInline(category: 3, sample: sample);

                // 尽量保证操作系统最少的进行内存页的换入与换出
                double dblMemoryUsageProportion = 100.00d * 
                    ((double)(sample.Context.TotalPhysicalMemory - sample.Context.AvailablePhysicalMemory) 
                    / sample.Context.TotalPhysicalMemory);
                if (dblMemoryUsageProportion > KEEP_PHYSICAL_MEMORY_PERCENTAGE)
                {
                    fMinSampleInline(category: 2, sample: sample);
                    continue;
                }

                fMinSampleInline(category: 1, sample: sample);

                // 尽量操作系统保留一定的CPU处理负载（以防止内核崩溃）
                if (sample.Context.CPULOAD > MIN_CPULOAD_ULTIMATE_NOT_EXCEEDING)
                {
                    continue;
                }

                fMinSampleInline(category: 0, sample: sample);
            }
            for (int iPriorityLevel = 0; iPriorityLevel < aoMinSample.Length; iPriorityLevel++)
            {
                NodeSample sample = aoMinSample[iPriorityLevel];
                if (sample != null)
                {
                    sampling = sample;
                    break;
                }
            }
            return Error.Error_Success;
        }
        /// <summary>
        /// 查询全部服务器
        /// </summary>
        /// <returns></returns>
        public virtual Error LookupAll(ApplicationType applicationType, out IEnumerable<NodeSample> samples)
        {
            IEnumerable<NodeSample> samplings = null;
            try
            {
                // 查询此类型的全部节点编号信息
                Error error = GetAllNodeid(applicationType, out HashSet<string> nodeid);
                if (error != Error.Error_Success)
                {
                    return error;
                }

                // 查询此类型的全部节点采样信息
                error = GetAllSampling(nodeid, out IDictionary<string, NodeSample> nodes);
                if (error != Error.Error_Success)
                {
                    return error;
                }

                samplings = nodes.Values;
                return Error.Error_Success;
            }
            finally
            {
                samples = samplings ?? m_aoNodeSampleEmpty;
            }
        }
        /// <summary>
        /// 负载均衡尽量保证CPU负载小于90%
        /// </summary>
        public const double MIN_CPULOAD_ULTIMATE_NOT_EXCEEDING = 90.00; // 
        /// <summary>
        /// 框架测量CPU负载达到95%(MAX_CPULOAD_ULTIMATE_NOT_EXCEEDING)时必须插入Sleep到工作线程（熔断器保护机制）
        ///     算法：
        ///         var sleep_time = (100 - MAX_CPULOAD_ULTIMATE_NOT_EXCEEDING) / Math.Min(MaxWorkThread, CPU CORE Number);
        ///         if (sleep_time <= 0) sleep_time = 1; // 最小在工作线程插入一个Sleep的时间（释放CPU）
        /// </summary>
        public const double MAX_CPULOAD_ULTIMATE_NOT_EXCEEDING = 95.00; 
        public const double MAX_NIC_AVAILABLE_BANDWIDTH_PERCENTAGE = 95.00;
        public const double KEEP_PHYSICAL_MEMORY_PERCENTAGE = 75.00;
        public const int KEEP_PHYSICAL_MEMORY_SIZE = 1 * 1024 * 1024 * 1024;
    }
}
