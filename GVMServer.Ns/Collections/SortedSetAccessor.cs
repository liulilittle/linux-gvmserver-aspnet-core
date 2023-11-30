namespace GVMServer.Ns.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using GVMServer.Linq;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using ServiceStack.Redis;

    public class SortedSetAccessor
    {
        public virtual decimal Chunking { get; } = 0;

        public static SortedSetAccessor Default { get; } = new SortedSetAccessor();

        private SortedSetAccessor() { }

        public SortedSetAccessor(decimal chunking)
        {
            if (chunking == 0)
            {
                throw new ArgumentOutOfRangeException("Chunking definitions cannot be equal than 0");
            }
            this.Chunking = chunking;
        }

        private string GetSortedSetKey<T>() => $"ns.configuration.center.sortedset.{this.Chunking}.collections.{typeof(T).FullName}";

        private string GetSortedKey<T>(IKey key) => GetSortedKey<T>(key?.GetKey());

        private string GetSortedKey<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            return $"ns.configuration.center.sortedset.{this.Chunking}.sortedkey.{typeof(T).FullName}.{key}";
        }

        private string GetKeyBySortedKey<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            string fk = $"ns.configuration.center.sortedset.{this.Chunking}.sortedkey.{typeof(T).FullName}.";
            int i = key.IndexOf(fk);
            if (i < 0)
            {
                return string.Empty;
            }
            string s = key.Substring(i + fk.Length);
            return s;
        }

        private string GetSynchronizeKey<T>() => $"ns.configuration.center.sortedset.{this.Chunking}.syncobjindex.{typeof(T).FullName}";

        private Error GetClient<T>(Func<IRedisClient, Error> handling, bool writing = true) => GetClient<T>(handling, 3, writing);

        private Error GetClient<T>(Func<IRedisClient, Error> handling, int timeout, bool writing)
        {
            if (!writing)
            {
                return CacheAccessor.GetClient((storage) => handling(storage));
            }
            return CacheAccessor.GetClient((storage) => CacheAccessor.AcquireLock(storage, GetSynchronizeKey<T>(), () => handling(storage), timeout));
        }

        public virtual Error Synchronize<T>(Action critical)
        {
            if (critical == null)
            {
                return Error.Error_SeriousCodingErrorsHandingMustBeStrictlyValidAndDoNotAllowTheUseOfAnyNullForm;
            }
            return GetClient<T>((storage) =>
            {
                critical();
                return Error.Error_Success;
            }, true);
        }

        public virtual long GetAllCount<T>(out Error error)
        {
            long counts = 0;
            error = GetClient<T>((storage) => CacheAccessor.GetSortedSetCount(storage, GetSortedSetKey<T>(), out counts));
            return counts;
        }

        public bool Remove<T>(int index, out Error error) => RemoveAll<T>(index, 1, out error) > 0;

        public long RemoveAll<T>(int index, out Error error) => RemoveAll<T>(index, -1, out error);

        public virtual long RemoveAll<T>(int index, int length, out Error error)
        {
            long events = 0;
            error = Error.Error_Success;
            if (index < 0 || length < ~0 || length == 0)
            {
                return events;
            }
            error = GetClient<T>((storage) =>
            {
                string kSet = GetSortedSetKey<T>();
                Error internalError = CacheAccessor.GetRangeFromSortedSet(storage, kSet, index, length, out List<string> kSortedKeys); // HR
                if (internalError != Error.Error_Success || kSortedKeys.IsNullOrEmpty())
                {
                    return internalError;
                }
                IRedisTransaction transaction = CacheAccessor.CreateTransaction(storage, out internalError);
                if (internalError != Error.Error_Success)
                {
                    return internalError;
                }
                int max = length;
                if (length != ~0)
                {
                    max = max + length;
                }
                transaction.QueueCommand(r => r.RemoveRangeFromSortedSet(kSet, index, max));
                if (kSortedKeys.Any())
                {
                    events = kSortedKeys.Count;
                    transaction.QueueCommand(r => r.RemoveAll(kSortedKeys));
                }
                return CacheAccessor.CommitedTransaction(transaction, internalError);
            });
            return events;
        }

        public IEnumerable<KeyValuePair<string, T>> GetAll<T>(int index, out Error error) => GetAll<T>(index, -1, out error);

        public virtual IEnumerable<KeyValuePair<string, T>> GetAll<T>(int index, int length, out Error error)
        {
            error = Error.Error_Success;
            IEnumerable<KeyValuePair<string, T>> defaults = new Dictionary<string, T>();
            if (index < 0 || length < ~0 || length == 0)
            {
                return defaults;
            }
            Error hr = Error.Error_Success;
            hr = GetClient<T>((storage) =>
            {
                string kSet = GetSortedSetKey<T>();
                hr = CacheAccessor.GetRangeFromSortedSet(storage, kSet, index, length, out List<string> kSortedKeys);
                if (hr != Error.Error_Success || kSortedKeys.IsNullOrEmpty())
                {
                    return hr;
                }
                hr = CacheAccessor.GetValues(storage, kSortedKeys, out IDictionary<string, T> s);
                if (hr != Error.Error_Success)
                {
                    return hr;
                }
                if (s.Any())
                {
                    defaults = s.Conversion(i => new KeyValuePair<string, T>(GetKeyBySortedKey<T>(i.Key), i.Value));
                }
                return Error.Error_Success;
            }, false);
            error = hr;
            return defaults;
        }

        public T Get<T>(int index, out Error error) => GetAll<T>(index, 1, out error).FirstOrDefault().Value;

        public virtual Error Clear<T>()
        {
            return GetClient<T>((storage) =>
            {
                string kSet = GetSortedSetKey<T>();
                Error internalError = CacheAccessor.GetRangeFromSortedSet(storage, kSet, 0, -1, out List<string> sortedkeys);
                if (internalError != Error.Error_Success || sortedkeys.IsNullOrEmpty())
                {
                    return internalError;
                }
                IRedisTransaction transaction = CacheAccessor.CreateTransaction(storage, out internalError);
                if (internalError != Error.Error_Success)
                {
                    return internalError;
                }
                transaction.QueueCommand(r => r.RemoveAll(sortedkeys));
                transaction.QueueCommand(r => r.Remove(kSet));
                return CacheAccessor.CommitedTransaction(transaction, internalError);
            });
        }

        public long IndexOf<T>(IKey key, out Error error) => IndexOf<T>(key?.GetKey(), out error);

        public virtual long IndexOf<T>(string key, out Error error)
        {
            error = Error.Error_Success;
            long indexes = ~0;
            if (string.IsNullOrEmpty(key))
            {
                return indexes;
            }
            error = GetClient<T>((storage) =>
            {
                indexes = CacheAccessor.GetItemIndexInSortedSet(storage, GetSortedSetKey<T>(), GetSortedKey<T>(key), out Error hr);
                return hr;
            }, false);
            return indexes;
        }

        public bool Delete<T>(IKey key, out Error error)
        {
            error = Error.Error_Success;
            if (key == null)
            {
                return false;
            }
            return DeleteAll<T>(new[] { key }, out error) > 0;
        }

        private class AnonymousKey : IKey
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string key = string.Empty;

            public AnonymousKey(string key) => this.key = key;

            public string GetKey() => this.key;
        }

        public bool Delete<T>(string key, out Error error) => Delete<T>(string.IsNullOrEmpty(key) ? default(AnonymousKey) : new AnonymousKey(key), out error);

        public virtual long DeleteAll<T>(IEnumerable<string> keys, out Error error) => DeleteAll<T>(keys.Conversion(i => string.IsNullOrEmpty(i) ? default(AnonymousKey) : new AnonymousKey(i)), out error);

        public virtual long DeleteAll<T>(IEnumerable<IKey> keys, out Error error)
        {
            long events = 0;
            error = Error.Error_Success;
            if (keys.IsNullOrEmpty())
            {
                return events;
            }
            Error internalError = Error.Error_Success;
            internalError = GetClient<T>(((storage) =>
            {
                IRedisTransaction transaction = CacheAccessor.CreateTransaction(storage, out internalError);
                if (transaction == null)
                {
                    return internalError;
                }
                string kSet = GetSortedSetKey<T>();
                List<string> sortedkeys = new List<string>();
                foreach (IKey key in keys)
                {
                    if (key == null)
                    {
                        continue;
                    }
                    string kElement = GetSortedKey<T>(key);
                    if (string.IsNullOrEmpty(kElement))
                    {
                        continue;
                    }
                    sortedkeys.Add(kElement);
                }
                if (sortedkeys.Count > 0)
                {
                    events = sortedkeys.Count;
                    transaction.QueueCommand(r => r.RemoveAll(sortedkeys));
                    transaction.QueueCommand(r => r.RemoveItemsFromSortedSet(kSet, sortedkeys));
                }
                return CacheAccessor.CommitedTransaction(transaction, internalError);
            }));
            error = internalError;
            if (error != Error.Error_Success)
            {
                events = 0;
            }
            return events;
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<IKey> keys, out Error error) => GetAll<T>(keys.Conversion(key => key?.GetKey()), out error);

        public virtual IEnumerable<T> GetAll<T>(IEnumerable<string> keys, out Error error)
        {
            error = Error.Error_Success;
            if (keys.IsNullOrEmpty())
            {
                return null;
            }
            IDictionary<string, T> rs = null;
            error = GetClient<T>((storage) => CacheAccessor.GetValues(storage, keys, out rs), false);
            return rs.Values;
        }

        public virtual IEnumerable<string> GetAllKeys<T>(int index, out Error error) => GetAllKeys<T>(index, -1, out error);

        public virtual IEnumerable<string> GetAllKeys<T>(int index, int length, out Error error)
        {
            error = Error.Error_Success;
            IEnumerable<string> defaults = new string[0];
            if (index < 0 || length < ~0 || length == 0)
            {
                return defaults;
            }
            Error hr = Error.Error_Success;
            hr = GetClient<T>((storage) =>
            {
                string kSet = GetSortedSetKey<T>();
                hr = CacheAccessor.GetRangeFromSortedSet(storage, kSet, index, length, out List<string> kSortedKeys);
                if (hr != Error.Error_Success || kSortedKeys.IsNullOrEmpty())
                {
                    return hr;
                }
                else
                {
                    defaults = kSortedKeys.Conversion(i => GetKeyBySortedKey<T>(i));
                }
                return Error.Error_Success;
            }, false);
            error = hr;
            return defaults;
        }

        public virtual Error Set<T>(int index, T value)
        {
            if (index < 0)
            {
                return Error.Error_TheRankingIndexMustNotBeLessThanZero;
            }
            return GetClient<T>((storage) =>
            {
                string kSet = GetSortedSetKey<T>();
                Error error = CacheAccessor.GetRangeFromSortedSet(storage, kSet, index, 1, out List<string> s);
                if (error != Error.Error_Success)
                {
                    return error;
                }
                return CacheAccessor.SetValue(storage, s.FirstOrDefault(), value);
            });
        }

        public T Get<T>(IKey key, out Error error) => Get<T>(key?.GetKey(), out error);

        public virtual T Get<T>(string key, out Error error)
        {
            error = Error.Error_Success;
            if (string.IsNullOrEmpty(key))
            {
                return default(T);
            }
            string kElement = GetSortedKey<T>(key);
            if (string.IsNullOrEmpty(kElement))
            {
                return default(T);
            }
            T obj = default(T);
            error = GetClient<T>((storage) => CacheAccessor.GetValue(storage, key, out obj), false);
            return obj;
        }

        public bool ContainsKey<T>(IKey key, out Error error) => ContainsKey<T>(key?.GetKey(), out error);

        public virtual bool ContainsKey<T>(string key, out Error error)
        {
            error = Error.Error_Success;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            string kElement = GetSortedKey<T>(key);
            if (string.IsNullOrEmpty(kElement))
            {
                return false;
            }
            bool contains = false;
            error = GetClient<T>((storage) => CacheAccessor.ContainsKey(storage, GetSortedKey<T>(key), out contains), false);
            return contains;
        }

        public Error Add<T>(T s) where T : IKey => AddAll(new[] { s });

        public virtual Error AddAll<T>(IEnumerable<T> s) where T : IKey => SetAll(s.Conversion(i => new KeyValuePair<string, T>(i?.GetKey(), i)));

        public Error Set<T>(string key, T value) => SetAll(new[] { new KeyValuePair<string, T>(key, value) });

        public virtual Error SetAll<T>(IEnumerable<KeyValuePair<string, T>> s)
        {
            if (s.IsNullOrEmpty())
            {
                return Error.Error_Success;
            }
            return GetClient<T>(((storage) =>
            {
                IRedisTransaction transaction = CacheAccessor.CreateTransaction(storage, out Error internalError);
                if (transaction == null)
                {
                    return internalError;
                }
                string kSet = GetSortedSetKey<T>();
                IDictionary<string, T> d = new Dictionary<string, T>();
                foreach (var kv in s)
                {
                    string key = kv.Key;
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }
                    string kElement = GetSortedKey<T>(key);
                    if (string.IsNullOrEmpty(kElement))
                    {
                        continue;
                    }
                    if (!d.TryGetValue(kElement, out T x) || x == null)
                    {
                        d[kElement] = kv.Value;
                        transaction.QueueCommand((r => r.AddItemToSortedSet(kSet, GetSortedKey<T>(key))));
                    }
                }
                if (d.Count > 0)
                {
                    transaction.QueueCommand(r => r.SetAll(d));
                }
                return CacheAccessor.CommitedTransaction(transaction, internalError);
            }));
        }
    }
}
