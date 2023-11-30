namespace GVMServer.Ns.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using GVMServer.Ns.Enum;
    using Enumerable = GVMServer.Linq.Enumerable;

    public class Dictionary<TValue> : IDictionary<string, TValue>
    {
        public BucketSet<TValue> Bucket { get; }

        public Dictionary(BucketSet<TValue> bucket)
        {
            this.Bucket = bucket ?? throw new ArgumentOutOfRangeException(nameof(bucket));
        }

        public virtual TValue this[string key]
        {
            get
            {
                return this.Bucket[key];
            }
            set
            {
                this.Bucket[key] = value;
            }
        }

        public virtual ICollection<string> Keys
        {
            get
            {
                return Enumerable.Collection(this.Bucket.Keys);
            }
        }

        public virtual ICollection<TValue> Values
        {
            get
            {
                return Enumerable.Collection(this.Bucket.Values);
            }
        }

        public virtual int Count
        {
            get
            {
                long counts = this.Bucket.Count;
                if (counts > int.MaxValue)
                {
                    counts = int.MaxValue;
                }
                return Convert.ToInt32(counts);
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public virtual void Add(string key, TValue value)
        {
            Error error = this.Bucket.Set(key, value);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
        }

        public virtual void Add(KeyValuePair<string, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public virtual void Clear()
        {
            Error error = this.Bucket.Clear();
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
        }

        public virtual bool Contains(KeyValuePair<string, TValue> item)
        {
            return this.ContainsKey(item.Key);
        }

        public virtual bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            bool containsKey = this.Bucket.ContainsKey(key, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            return containsKey;
        }

        public virtual void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            this.Bucket.CopyTo(array, arrayIndex, kv => kv);
        }

        public virtual IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            IEnumerable<KeyValuePair<string, TValue>> bucket = this.Bucket;
            return bucket.GetEnumerator();
        }

        public virtual bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            bool success = this.Bucket.Remove(key, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            return success;
        }

        public virtual bool Remove(KeyValuePair<string, TValue> item)
        {
            return this.Remove(item.Key);
        }

        public virtual bool TryGetValue(string key, out TValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = default(TValue);
                return false;
            }
            value = this.Bucket.Get(key, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
