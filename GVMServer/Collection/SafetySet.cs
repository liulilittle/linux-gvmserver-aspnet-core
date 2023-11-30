namespace GVMServer.Collection
{
    using System.Collections;
    using System.Collections.Generic;

    public class SafetySet<T> : ISet<T>
    {
        private ISet<T> sets = new HashSet<T>();

        public int Count
        {
            get
            {
                return sets.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return sets.IsReadOnly;
            }
        }

        public bool Add(T item)
        {
            lock (this)
                return sets.Add(item);
        }

        public void Clear()
        {
            lock (this)
                sets.Clear();
        }

        public bool Contains(T item)
        {
            lock (this)
                return sets.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (this)
                sets.CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            lock (this)
                sets.ExceptWith(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            IList<T> s = new List<T>(sets.Count);
            lock (this)
            {
                foreach (T t in sets)
                    s.Add(t);
            }
            return s.GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            lock (this)
                sets.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            lock (this)
                return sets.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            lock (this)
                return sets.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            lock (this)
                return sets.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            lock (this)
                return sets.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            lock (this)
                return sets.Overlaps(other);
        }

        public bool Remove(T item)
        {
            lock (this)
                return sets.Remove(item);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            lock (this)
                return sets.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            lock (this)
                sets.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            lock (this)
                sets.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            lock (this)
                sets.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
