namespace GVMServer.Collection
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public abstract class MulticastArray<T> : IEnumerable<T>
    {
        private readonly IDictionary<int, ICollection<T>> _ss = new Dictionary<int, ICollection<T>>();

        protected ICollection<T> GetCurrentCollection()
        {
            lock (this)
            {
                int key = Thread.CurrentThread.ManagedThreadId;
                ICollection<T> ss = null;
                if (!_ss.TryGetValue(key, out ss))
                {
                    ss = new HashSet<T>();
                    _ss.Add(key, ss);
                }
                return ss;
            }
        }

        protected abstract ICollection<T> NewCollection();

        public virtual bool Contains(T item)
        {
            lock (this)
            {
                return GetCurrentCollection().Contains(item);
            }
        }

        public virtual void Add(T item)
        {
            lock (this)
            {
                GetCurrentCollection().Add(item);
            }
        }

        public virtual bool Remove(T item)
        {
            lock (this)
            {
                return GetCurrentCollection().Remove(item);
            }
        }

        public virtual void Clear()
        {
            lock (this)
            {
                ICollection<T> set = GetCurrentCollection();
                if (set.Count > 0)
                {
                    set.Clear();
                }
            }
        }

        public virtual int Count
        {
            get
            {
                lock (this)
                {
                    return GetCurrentCollection().Count;
                }
            }
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            ICollection<T> set = GetCurrentCollection();
            lock (set)
            {
                foreach (T obj in set)
                {
                    yield return obj;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ICollection<T> set = GetCurrentCollection();
            lock (set)
            {
                foreach (T obj in set)
                {
                    yield return obj;
                }
            }
        }
    }
}
