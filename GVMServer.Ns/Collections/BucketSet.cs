namespace GVMServer.Ns.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using GVMServer.Linq;
    using GVMServer.Ns.Enum;
    /// <summary>
    /// 运行在完全分布式环境多个节点上双向Z跳表/跳链散列计算结构型式有序集合容器(ZSet)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BucketSet<T> : IEnumerable<KeyValuePair<string, T>>
    {
        public SortedSetAccessor Accessor { get; }

        public BucketSet() : this(SortedSetAccessor.Default)
        {

        }

        public BucketSet(SortedSetAccessor accessor)
        {
            this.Accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        public virtual Error Synchronize(Action critical) => this.Accessor.Synchronize<T>(critical);
        
        public virtual bool Remove(int index, out Error error) => this.Accessor.Remove<T>(index, out error);

        public virtual long RemoveAll(out Error error) => this.Accessor.RemoveAll<T>(0, out error);

        public virtual long RemoveAll(int index, out Error error) => this.Accessor.RemoveAll<T>(index, out error);

        public virtual long RemoveAll(int index, int length, out Error error) => this.Accessor.RemoveAll<T>(index, length, out error);

        public virtual long RemoveAll(IEnumerable<string> keys, out Error error) => this.Accessor.DeleteAll<T>(keys, out error);

        public virtual IEnumerable<KeyValuePair<string, T>> GetAll(out Error error) => this.Accessor.GetAll<T>(0, out error);

        public virtual IEnumerable<KeyValuePair<string, T>> GetAll(int index, out Error error) => this.Accessor.GetAll<T>(index, out error);

        public virtual IEnumerable<KeyValuePair<string, T>> GetAll(int index, int length, out Error error) => this.Accessor.GetAll<T>(index, length, out error);

        public virtual T Get(int index, out Error error) => this.Accessor.Get<T>(index, out error);

        public virtual Error Clear() => this.Accessor.Clear<T>();

        public virtual long RemoveAll(IEnumerable<IKey> keys, out Error error) => this.Accessor.DeleteAll<T>(keys, out error);

        public virtual bool Remove(IKey key, out Error error) => this.Accessor.Delete<T>(key, out error);

        public virtual bool Remove(string key, out Error error) => this.Accessor.Delete<T>(key, out error);

        public virtual long Remove(IEnumerable<string> keys, out Error error) => this.Accessor.DeleteAll<T>(keys, out error);

        public virtual IEnumerable<T> GetAll(IEnumerable<IKey> keys, out Error error) => this.Accessor.GetAll<T>(keys, out error);

        public virtual IEnumerable<T> GetAll(IEnumerable<string> keys, out Error error) => this.Accessor.GetAll<T>(keys, out error);

        public virtual T Get(IKey key, out Error error) => this.Accessor.Get<T>(key, out error);

        public virtual T Get(string key, out Error error) => this.Accessor.Get<T>(key, out error);

        public virtual bool ContainsKey(string key, out Error error) => this.Accessor.ContainsKey<T>(key, out error);

        public virtual bool ContainsKey(IKey key, out Error error) => this.Accessor.ContainsKey<T>(key, out error);

        public virtual Error Add<E>(E s) where E : IKey => this.Accessor.Add(s);

        public virtual Error AddAll<E>(IEnumerable<E> s) where E : IKey => this.Accessor.AddAll(s);

        public virtual Error Set(string key, T value) => this.Accessor.Set(key, value);

        public virtual Error Set(int index, T value) => this.Accessor.Set(index, value);

        public virtual Error SetAll(IEnumerable<KeyValuePair<string, T>> s) => this.Accessor.SetAll(s);

        public virtual IEnumerable<string> GetAllKeys(out Error error) => this.Accessor.GetAllKeys<T>(0, out error);

        public virtual IEnumerable<string> GetAllKeys(int index, out Error error) => this.Accessor.GetAllKeys<T>(index, out error);

        public virtual IEnumerable<string> GetAllKeys(int index, int length, out Error error) => this.Accessor.GetAllKeys<T>(0, length, out error);

        public virtual long IndexOf(string key, out Error error) => this.Accessor.IndexOf<T>(key, out error);

        public virtual long IndexOf(IKey key, out Error error) => this.Accessor.IndexOf<T>(key, out error);

        public virtual IEnumerator<T> GetEnumerator()
        {
            var s = this.GetAll(0, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            else
            {
                if (s == null)
                {
                    IEnumerable<T> defaults = new T[0];
                    return defaults.GetEnumerator();
                }
                return s.Conversion(kv => kv.Value).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
        {
            var s = this.GetAll(0, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            else
            {
                if (s == null)
                {
                    IEnumerable<KeyValuePair<string, T>> defaults = new KeyValuePair<string, T>[0];
                    return defaults.GetEnumerator();
                }
                return s.GetEnumerator();
            }
        }

        public virtual long Count
        {
            get
            {
                long results = this.Accessor.GetAllCount<T>(out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                return results;
            }
        }

        public virtual IEnumerable<string> Keys
        {
            get
            {
                var keys = GetAllKeys(out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                return keys ?? new string[0];
            }
        }

        public virtual IEnumerable<T> Values
        {
            get
            {
                var s = GetAll(out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                return s.Conversion(kv => kv.Value) ?? new T[0];
            }
        }

        public virtual T this[string key]
        {
            get
            {
                T r = Get(key, out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                return r;
            }
            set
            {
                Error error = Set(key, value);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
            }
        }

        public virtual T this[int index]
        {
            get
            {
                T r = Get(index, out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                return r;
            }
            set
            {
                Error error = Set(index, value);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
            }
        }

        public virtual void CopyTo<R>(R[] array, int arrayIndex, Func<KeyValuePair<string, T>, R> conversion)
        {
            if (conversion == null)
            {
                throw new ArgumentNullException(nameof(conversion));
            }
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            int counts = unchecked(array.Length - arrayIndex);
            if (counts > 0)
            {
                var sources = this.GetAll(0, counts, out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                if (sources != null)
                {
                    int offset = arrayIndex;
                    foreach (KeyValuePair<string, T> kv in sources)
                    {
                        if (offset >= array.Length)
                        {
                            break;
                        }
                        array[offset++] = conversion(kv);
                    }
                }
            }
        }
    }
}
