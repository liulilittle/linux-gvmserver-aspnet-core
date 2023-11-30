namespace GVMServer.Ns.Functional
{
    using System;
    using System.Collections.Generic;
    using GVMServer.Cache;
    using GVMServer.Linq;
    using GVMServer.Ns.Enum;
    using ServiceStack.Redis;
    using ServiceStack.Redis.Pipeline;

    public static class CacheAccessor
    {
        public static Error Unlock(IDisposable locker)
        {
            try
            {
                if (locker != null)
                {
                    locker.Dispose();
                }

                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_ProblemsOccurredInReleasingTheDistributedCriticalSectionBlockLocks;
            }
        }

        public static Error Dispose(IRedisClient redis, Error error = Error.Error_Success)
        {
            try
            {
                if (redis != null)
                {
                    redis.Dispose();
                }

                return Error.Error_Success;
            }
            catch (Exception)
            {
                return error != Error.Error_Success ? error : Error.Error_UnableToDisposingRedisClientObject;
            }
        }

        public static Error GetClient(out IRedisClient redis)
        {
            redis = null;
            try
            {
                redis = RedisClientManager.GetDefault().GetClient();
                if (redis == null)
                {
                    return Error.Error_AnUncomprehendingErrorThatGoesBeyondTheScopeOfAGivenDesignAndFailsToObtainAReferenceToTheRedisClient;
                }
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_TheRedisClientCouldNotBeRetrievedFromTheConnectionPoolManager;
            }
        }

        public static Error GetClient(Func<IRedisClient, Error> handing)
        {
            Error error = Error.Error_Success;
            if (handing == null)
            {
                return Error.Error_SeriousCodingErrorsHandingMustBeStrictlyValidAndDoNotAllowTheUseOfAnyNullForm;
            }
            IRedisClient redis = null;
            try
            {
                var pool = RedisClientManager.GetDefault();
                if (pool == null)
                {
                    error = Error.Error_TheRedisClientCouldNotBeRetrievedFromTheConnectionPoolManager;
                }
                else
                {
                    redis = pool.GetClient();
                    if (redis == null)
                    {
                        error = Error.Error_AnUncomprehendingErrorThatGoesBeyondTheScopeOfAGivenDesignAndFailsToObtainAReferenceToTheRedisClient;
                    }
                    else
                    {
                        try
                        {
                            error = handing(redis);
                        }
                        catch (Exception)
                        {
                            error = Error.Error_SeriousInternalUnhandledExceptionProblems;
                        }
                        error = CacheAccessor.Dispose(redis, error);
                    }
                }
            }
            catch (Exception)
            {
                error = Error.Error_TheRedisClientCouldNotBeRetrievedFromTheConnectionPoolManager;
            }
            return error;
        }

        public static Error AcquireLock(IRedisClient redis, string key, Func<Error> handling, int timeout = 3)
        {
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            if (handling == null)
            {
                return Error.Error_SeriousCodingErrorsHandingMustBeStrictlyValidAndDoNotAllowTheUseOfAnyNullForm;
            }
            Error error = AcquireLock(redis, key, out IDisposable disposable, timeout);
            if (error != Error.Error_Success)
            {
                return error;
            }
            try
            {
                error = handling();
            }
            catch (Exception)
            {
                if (error == Error.Error_Success)
                {
                    error = Error.Error_SeriousCodingErrorsHandingMustBeStrictlyValidAndDoNotAllowTheUseOfAnyNullForm;
                }
            }
            try
            {
                disposable?.Dispose();
            }
            catch (Exception) { }
            return error;
        }

        public static Error AcquireLock(IRedisClient redis, string key, out IDisposable locker, int timeout = 3)
        {
            locker = null;
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            try
            {
                if (timeout == 0)
                {
                    timeout = 10;
                }
                if (timeout < 0)
                {
                    locker = redis.AcquireLock(key);
                }
                else
                {
                    locker = redis.AcquireLock(key, new TimeSpan(0, 0, timeout));
                }
            }
            catch (TimeoutException) // RedisTimeoutException
            {
                return Error.Error_UnableToAcquireLockNowIsWaitingTimeoutException;
            }
            catch (Exception)
            {
                return Error.Error_TheDistributedCriticalSectionBlockLockCannotBeObtained;
            }
            return Error.Error_Success;
        }

        public static Error ContainsKey(IRedisClient redis, string key, out bool contains)
        {
            contains = false;
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_Success;
            }
            try
            {
                contains = redis.ContainsKey(key);
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_GetRedisCacheToLocalCacheTimeThrowException;
            }
        }

        public static Error GetValue<T>(IRedisClient redis, string key, out T value)
        {
            value = default(T);
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            try
            {
                value = redis.Get<T>(key);
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_GetRedisCacheToLocalCacheTimeThrowException;
            }
        }

        public static Error GetValues<T>(IRedisClient redis, IEnumerable<string> keys, out IDictionary<string, T> s)
        {
            s = null;
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (keys.IsNullOrEmpty())
            {
                return Error.Error_Success;
            }
            try
            {
                s = redis.GetAll<T>(keys);
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_UnableToGetAllItemsFromSetTheDistributedCache;
            }
        }

        public static Error SetValue<T>(IRedisClient redis, string key, T value)
        {
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            try
            {
                if (!redis.Set(key, value))
                {
                    return Error.Error_SuccessSetToRedisCacheMemoryServerButIsReturnFailtrue;
                }
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_UnableToSetRedisCacheToMemoryServer;
            }
        }

        public static Error Remove(IRedisClient redis, string key)
        {
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            try
            {
                if (!redis.Remove(key))
                {
                    return Error.Error_SuccessRemoveToRedisCacheMemoryServerButIsReturnFailtrue;
                }
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_UnableToRemoveInRedisCacheToMemoryServer;
            }
        }

        public static Error RemoveAll(IRedisClient redis, IEnumerable<string> keys)
        {
            if (keys.IsNullOrEmpty())
            {
                return Error.Error_Success;
            }
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            try
            {
                redis.RemoveAll(keys);
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_UnableToRemoveInRedisCacheToMemoryServer;
            }
        }

        public static Error GetSortedSetCount(IRedisClient redis, string key, out long count)
        {
            count = 0;
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_KeyNotAllowIsANullOrEmptyString;
            }
            try
            {
                count = redis.GetSortedSetCount(key);
                if (count <= 0)
                    count = 0;
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_AnUnknownRedisExceptionOccurredWhileObtainingTheNumberOfMembersOfTheZSetCollection;
            }
        }

        public static Error RollbackTransaction(IRedisTransaction transaction, Error error = Error.Error_Success)
        {
            if (transaction == null)
            {
                return error;
            }
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
            return error;
        }

        public static Error CommitedTransaction(IRedisTransaction transaction, Error error = Error.Error_Success)
        {
            if (transaction == null)
            {
                return error;
            }
            bool rollback = error != Error.Error_Success;
            try
            {
                if (!rollback)
                {
                    try
                    {
                        transaction.Commit();
                        return Error.Error_Success;
                    }
                    catch (Exception)
                    {
                        rollback = true;
                    }
                }
                if (rollback)
                {
                    RollbackTransaction(transaction, error);
                }
            }
            finally
            {
                if (transaction != null)
                {
                    try
                    {
                        transaction.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            return error;
        }

        public static IRedisTransaction CreateTransaction(IRedisClient redis, out Error error)
        {
            if (redis == null)
            {
                error = Error.Error_YourInputRedisInstanceIsNullReferences;
                return null;
            }
            try
            {
                IRedisTransaction transaction = redis.CreateTransaction();
                if (transaction == null)
                    error = Error.Error_TheTransactionInstanceOfRedisCannotBeOpened;
                else
                    error = Error.Error_Success;
                return transaction;
            }
            catch (Exception)
            {
                error = Error.Error_TheTransactionInstanceOfRedisCannotBeOpened;
                return null;
            }
        }

        public static Error FlushPipeline(IRedisPipeline pipeline, Error error = Error.Error_Success)
        {
            if (pipeline == null)
            {
                return error;
            }
            try
            {
                if (error == Error.Error_Success)
                {
                    try
                    {
                        pipeline.Flush();
                        return Error.Error_Success;
                    }
                    catch (Exception)
                    {
                        return Error.Error_TheFlushPipelineToTheRedisCacheFailedButThereWasAProblem;
                    }
                }
            }
            finally
            {
                if (pipeline != null)
                {
                    try
                    {
                        pipeline.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            return error;
        }

        public static Error ZRangeBylex(IRedisClient redis, string key, string min, string max, int startIndex, int length, out IEnumerable<string> s)
        {
            return ZRevWithRangeBylex(redis, key, min, max, startIndex, length, out s, false);
        }

        public static Error ZRevrangeBylex(IRedisClient redis, string key, string min, string max, int startIndex, int length, out IEnumerable<string> s)
        {
            return ZRevWithRangeBylex(redis, key, min, max, startIndex, length, out s, true);
        }

        private static Error ZRevWithRangeBylex(IRedisClient redis, string key, string min, string max, int startIndex, int length, out IEnumerable<string> s, bool zrevrangebylex)
        {
            s = null;
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            RedisText rt = null;
            try
            {
                string commands = zrevrangebylex ? "ZREVRANGEBYLEX" : "ZRANGEBYLEX";
                if (startIndex < 0)
                {
                    startIndex = 0;
                }
                if (zrevrangebylex)
                {
                    rt = redis.Custom(commands, key, max, min, "LIMIT", startIndex, length);
                }
                else
                {
                    rt = redis.Custom(commands, key, min, max, "LIMIT", startIndex, length);
                }
                if (rt == null)
                {
                    return Error.Error_ANullRedisTextIsObtainedAtTheEndOfTheRequestToTheRemoteCacheServer;
                }
            }
            catch (Exception)
            {
                return Error.Error_UnableToRequestZrevrangebylexOrZrangebylexCommandFromTheCacheServer;
            }
            if (rt != null)
            {
                s = rt.Children.Conversion(i => i.Text);
            }
            return Error.Error_Success;
        }

        public static IRedisPipeline CreatePipeline(IRedisClient redis, out Error error)
        {
            if (redis == null)
            {
                error = Error.Error_YourInputRedisInstanceIsNullReferences;
                return null;
            }
            try
            {
                IRedisPipeline transaction = redis.CreatePipeline();
                if (transaction == null)
                    error = Error.Error_ThePipelineInstanceOfRedisCannotBeOpened;
                else
                    error = Error.Error_Success;
                return transaction;
            }
            catch (Exception)
            {
                error = Error.Error_ThePipelineInstanceOfRedisCannotBeOpened;
                return null;
            }
        }

        public static long GetItemIndexInSortedSet(IRedisClient redis, string key, string value, out Error error) => InternalGetItemIndexInSortedSet(redis, key, value, out error, true);

        public static long GetItemIndexInSortedSetDesc(IRedisClient redis, string key, string value, out Error error) => InternalGetItemIndexInSortedSet(redis, key, value, out error, false);

        private static long InternalGetItemIndexInSortedSet(IRedisClient redis, string key, string value, out Error error, bool ascending)
        {
            error = Error.Error_Success;
            if (string.IsNullOrEmpty(key))
            {
                error = Error.Error_YourInputAkeyCannotBeAnEmptyString;
                return ~0;
            }
            if (redis == null)
            {
                error = Error.Error_YourInputRedisInstanceIsNullReferences;
                return ~0;
            }
            if (string.IsNullOrEmpty(value))
            {
                return ~0;
            }
            try
            {
                if (ascending)
                    return redis.GetItemIndexInSortedSet(key, value);
                return redis.GetItemIndexInSortedSetDesc(key, value);
            }
            catch (Exception)
            {
                error = Error.Error_UnableToExecuteGetItemIndexInSortedSet;
                return ~0;
            }
        }

        private enum FindSortedSetRangeItemsByMode
        {
            GetRangeWithScoresFromSortedSetDesc,
            GetRangeFromSortedSetDesc,
            GetRangeWithScoresFromSortedSet,
            GetRangeFromSortedSet,
        }

        private static Error FindCollectionItemsByMode<T>(IRedisClient redis, string key, int index, int length, out T result, FindSortedSetRangeItemsByMode mode)
        {
            result = default(T);
            if (index < 0)
            {
                return Error.Error_TheRankingIndexMustNotBeLessThanZero;
            }
            if (string.IsNullOrEmpty(key))
            {
                return Error.Error_YourInputAkeyCannotBeAnEmptyString;
            }
            if (redis == null)
            {
                return Error.Error_YourInputRedisInstanceIsNullReferences;
            }
            int to = length >= 0 ? (index + length) - 1 : -1;
            try
            {
                switch (mode)
                {
                    case FindSortedSetRangeItemsByMode.GetRangeWithScoresFromSortedSetDesc:
                        result = (T)(object)redis.GetRangeWithScoresFromSortedSetDesc(key, index, to);
                        break;
                    case FindSortedSetRangeItemsByMode.GetRangeFromSortedSetDesc:
                        result = (T)(object)redis.GetRangeFromSortedSetDesc(key, index, to);
                        break;
                    case FindSortedSetRangeItemsByMode.GetRangeWithScoresFromSortedSet:
                        result = (T)(object)redis.GetRangeWithScoresFromSortedSet(key, index, to);
                        break;
                    case FindSortedSetRangeItemsByMode.GetRangeFromSortedSet:
                        result = (T)(object)redis.GetRangeFromSortedSet(key, index, to);
                        break;
                };
                return Error.Error_Success;
            }
            catch (Exception)
            {
                return Error.Error_ThereWasAnUnexpectedProblemGettingRankingMemberForASliceFromTheShardingStore;
            }
        }

        public static Error GetRangeFromSortedSet(IRedisClient redis, string key, int index, int length, out List<string> s)
            => FindCollectionItemsByMode(redis, key, index, length, out s, FindSortedSetRangeItemsByMode.GetRangeFromSortedSet);

        public static Error GetRangeWithScoresFromSortedSet(IRedisClient redis, string key, int index, int length, out List<string> s)
            => FindCollectionItemsByMode(redis, key, index, length, out s, FindSortedSetRangeItemsByMode.GetRangeWithScoresFromSortedSet);

        public static Error GetRangeFromSortedSetDesc(IRedisClient redis, string key, int index, int length, out List<string> s)
            => FindCollectionItemsByMode(redis, key, index, length, out s, FindSortedSetRangeItemsByMode.GetRangeFromSortedSetDesc);

        public static Error GetRangeWithScoresFromSortedSetDesc(IRedisClient redis, string key, int index, int length, out IDictionary<string, double> s)
            => FindCollectionItemsByMode(redis, key, index, length, out s, FindSortedSetRangeItemsByMode.GetRangeWithScoresFromSortedSetDesc);
    }
}
