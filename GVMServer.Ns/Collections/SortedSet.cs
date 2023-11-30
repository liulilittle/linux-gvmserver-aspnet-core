namespace GVMServer.Ns.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using GVMServer.Ns.Enum;

    public class SortedSet<T> : ISet<T> where T : IKey
    {
        public BucketSet<T> Bucket { get; }

        public SortedSet(BucketSet<T> bucket)
        {
            this.Bucket = bucket ?? throw new ArgumentOutOfRangeException(nameof(bucket));
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

        public virtual bool Add(T item)
        {
            ICollection<T> self = this;
            self.Add(item);
            return true;
        }

        public virtual void Clear()
        {
            Error error = this.Bucket.Clear();
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
        }

        public virtual bool Contains(T item)
        {
            if (item == null)
            {
                return false;
            }
            bool containsKey = this.Bucket.ContainsKey(item, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            return containsKey;
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            this.Bucket.CopyTo(array, arrayIndex, kv => kv.Value);
        }

        public virtual void ExceptWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return this.Bucket.GetEnumerator();
        }

        public virtual void IntersectWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool Overlaps(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool Remove(T item)
        {
            bool success = this.Bucket.Remove(item, out Error error);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
            return success;
        }

        public virtual bool SetEquals(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public virtual void UnionWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        void ICollection<T>.Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            Error error = this.Bucket.Add(item);
            if (error != Error.Error_Success)
            {
                throw new RuntimeException(error);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
