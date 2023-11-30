namespace GVMServer.Collection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class SafetyList<T> : IList<T>
    {
        private object locker = new object();
        private IList<T> buffer = null;

        public SafetyList()
        {
            buffer = new List<T>();
        }

        public SafetyList(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }
            if (list is T[])
            {
                IList<T> s = new List<T>();
                foreach (T i in s)
                {
                    s.Add(i);
                }
                list = s;
            }
            buffer = list;
        }

        public SafetyList(int capacity)
        {
            buffer = new List<T>(capacity);
        }

        public T this[int index]
        {
            get
            {
                lock (locker)
                {
                    return buffer[index];
                }
            }
            set
            {
                lock (locker)
                {
                    buffer[index] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                return buffer.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return buffer.IsReadOnly;
            }
        }

        public void Add(T item)
        {
            lock (locker)
            {
                buffer.Add(item);
            }
        }

        public void Clear()
        {
            lock (locker)
            {
                buffer.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (locker)
            {
                return buffer.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (locker)
            {
                buffer.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (locker)
            {
                IList<T> s = new List<T>(buffer.Count);
                for (int i = 0; i < buffer.Count; i++)
                {
                    s.Add(buffer[i]);
                }
                return s.GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (locker)
            {
                return buffer.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (locker)
            {
                buffer.Insert(index, item);
            }
        }

        public bool Remove(T item)
        {
            lock (locker)
            {
                return buffer.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (locker)
            {
                buffer.RemoveAt(index);
            }
        }

        public void AddRange(SafetyList<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            lock (locker)
            {
                foreach (var item in collection)
                {
                    buffer.Add(item);
                }
            }
        }

        public static SafetyList<T> Wrapper(IList<T> value)
        {
            if (value == null)
            {
                value = new List<T>();
            }
            return new SafetyList<T>(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            int i = 0;
            while (true)
            {
                T value = default(T);
                lock (locker)
                {
                    if (i >= buffer.Count)
                    {
                        break;
                    }
                    else
                    {
                        value = buffer[i];
                    }
                }
                i++;
                yield return value;
            }
        }
    }
}
