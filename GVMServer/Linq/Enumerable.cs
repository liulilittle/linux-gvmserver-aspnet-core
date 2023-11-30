namespace GVMServer.Linq
{
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using GVMServer.DDD.Service.Component;

    public static class Enumerable
    {
        public static bool IsNullOrEmpty(this IEnumerable enumerable)
        {
            if (enumerable == null)
                return true;
            return IsNullOrEmpty(enumerable.GetEnumerator());
        }

        public static bool IsNullOrEmpty(this IEnumerator enumerator)
        {
            if (enumerator == null)
                return true;
            return !enumerator.MoveNext();
        }

        public static IEnumerable<T> EmptyArray<T>()
        {
            return new HashSet<T>();
        }

        public static bool Any(this IEnumerator enumerator)
        {
            return !IsNullOrEmpty(enumerator);
        }

        public static bool Any(this IEnumerable enumerable)
        {
            if (enumerable == null)
                return false;
            return Any(enumerable.GetEnumerator());
        }

        public static T FirstOfDefault<T>(this IEnumerator<T> enumerator, Func<T, bool> equals)
        {
            if (enumerator == null)
            {
                return default(T);
            }
            if (equals == null)
            {
                if (!enumerator.MoveNext())
                {
                    return default(T);
                }
                return enumerator.Current;
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    if (equals(enumerator.Current))
                    {
                        return enumerator.Current;
                    }
                }
            }
            return default(T);
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> enumerable, Func<T, bool> equals)
        {
            if (enumerable == null)
            {
                return default(T);
            }
            return FirstOfDefault(enumerable.GetEnumerator(), equals);
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> enumerable)
        {
            return FirstOrDefault(enumerable, null);
        }

        public static T FirstOrDefault<T>(this IEnumerator<T> enumerator)
        {
            return FirstOfDefault(enumerator, null);
        }

        public static IEnumerable<T> Where<T>(this IEnumerator<T> enumerator, Func<T, bool> predicate)
        {
            IList<T> s = new List<T>();
            if (enumerator != null)
            {
                while (enumerator.MoveNext())
                {
                    if (predicate == null || predicate(enumerator.Current))
                    {
                        s.Add(enumerator.Current);
                    }
                }
            }
            return s;
        }

        public static IEnumerable<T> Where<T>(this IEnumerator<T> enumerator)
        {
            return Where<T>(enumerator, null);
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> enumerable)
        {
            return Where(enumerable, null);
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            if (enumerable == null)
                return new List<T>();
            return Where(enumerable.GetEnumerator(), predicate);
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerator<TSource> enumerator, Func<TSource, TResult> selector)
        {
            IList<TResult> results = new List<TResult>();
            if (enumerator == null)
            {
                return results;
            }
            while (enumerator.MoveNext())
            {
                if (selector == null)
                    results.Add(default(TResult));
                else
                    results.Add(selector(enumerator.Current));
            }
            return results;
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> selector)
        {
            if (enumerable == null)
            {
                return new List<TResult>();
            }
            return Select<TSource, TResult>(enumerable.GetEnumerator(), selector);
        }

        public static List<T> ToList<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return new List<T>();
            return new List<T>(enumerable);
        }

        public static List<T> ToList<T>(this IEnumerator<T> enumerator)
        {
            List<T> s = new List<T>();
            if (enumerator != null)
            {
                while (enumerator.MoveNext())
                {
                    s.Add(enumerator.Current);
                }
            }
            return s;
        }

        public static T[] ToArray<T>(this IEnumerable<T> enumerable)
        {
            return ToList<T>(enumerable).ToArray();
        }

        public static T[] ToArray<T>(this IEnumerator<T> enumerator)
        {
            return ToList(enumerator).ToArray();
        }

        public static int Count<T>(this IEnumerator<T> enumerator)
        {
            if (enumerator == null)
                return 0;
            int count = 0;
            while (enumerator.MoveNext())
                count++;
            return count;
        }

        public static int Count<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return 0;
            ICollection<T> collection = enumerable as ICollection<T>;
            if (collection != null)
                return collection.Count;
            return Count(enumerable.GetEnumerator());
        }

        public static IEnumerable<T> Reverse<T>(this IEnumerator<T> enumerator)
        {
            List<T> s = new List<T>();
            if (enumerator == null)
                return s;
            while (enumerator.MoveNext())
            {
                T current = enumerator.Current;
                s.Insert(0, current);
            }
            return s;
        }

        public static IEnumerable<T> Reverse<T>(this IEnumerable<T> enumerable)
        {
            return Reverse(enumerable);
        }

        public static bool Contains<T>(this IEnumerator<T> enumerator, Func<T, bool> equals)
        {
            if (enumerator == null || equals == null)
            {
                return false;
            }
            while (enumerator.MoveNext())
            {
                if (equals(enumerator.Current))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Contains<T>(this IEnumerable<T> enumerable, Func<T, bool> equals)
        {
            if (enumerable == null)
            {
                return false;
            }
            return Contains(enumerable.GetEnumerator(), equals);
        }

        public static bool Contains<T>(this IEnumerable<T> enumerable, T item)
        {
            if (enumerable == null)
            {
                return false;
            }
            if (enumerable is ICollection<T> ct)
            {
                return ct.Contains(item);
            }
            if (enumerable is IList co)
            {
                return co.Contains(item);
            }
            object mo = (object)item;
            foreach (var io in enumerable)
            {
                if ((object)io == mo)
                {
                    return true;
                }
                if (object.Equals(io, item))
                {
                    return true;
                }
            }
            return false;
        }

        public static int Dispose<T>(this SIoC<T> ioC)
            where T : class
        {
            if (ioC == null)
            {
                return 0;
            }

            ICollection<T> container = ioC.Container as ICollection<T>;
            if (container == null)
            {
                return 0;
            }

            ISet<T> sets = new HashSet<T>();
            foreach (var o in container)
            {
                IDisposable disposable = o as IDisposable;
                if (disposable == null)
                {
                    continue;
                }

                sets.Add(o);
                disposable.Dispose();
            }

            foreach (var o in sets)
            {
                container.Remove(o);
            }

            return 0;
        }

        public class WrapperEnumerable<T, TResult> : IEnumerable<TResult>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IEnumerable<T> m_poEnumerable;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private Func<T, TResult> m_pfnConversion;

            public WrapperEnumerable(IEnumerable<T> enumerable, Func<T, TResult> conversion)
            {
                this.m_poEnumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
                this.m_pfnConversion = conversion ?? throw new ArgumentNullException(nameof(conversion));
            }

            public virtual IEnumerable<T> Owner
            {
                get
                {
                    return m_poEnumerable;
                }
            }

            public IEnumerator<TResult> GetEnumerator()
            {
                return new WrapperEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            private class WrapperEnumerator : IEnumerator<TResult>
            {
                private readonly WrapperEnumerable<T, TResult> m_poSelf;
                private readonly IEnumerator<T> m_poEnumerator;
                private TResult m_vtCurrent;

                public WrapperEnumerator(WrapperEnumerable<T, TResult> enumerable)
                {
                    m_poSelf = enumerable;
                    m_poEnumerator = enumerable.m_poEnumerable.GetEnumerator();
                }

                public TResult Current => m_vtCurrent;

                object IEnumerator.Current => m_vtCurrent;

                public void Dispose()
                {
                    lock (this)
                    {
                        m_poEnumerator.Dispose();
                    }
                }

                public bool MoveNext()
                {
                    lock (this)
                    {
                        bool bMoveNext = m_poEnumerator.MoveNext();
                        if (!bMoveNext)
                        {
                            m_vtCurrent = default(TResult);
                        }
                        else
                        {
                            m_vtCurrent = m_poSelf.m_pfnConversion(m_poEnumerator.Current);
                        }
                        return bMoveNext;
                    }
                }

                public void Reset()
                {
                    lock (this)
                    {
                        m_poEnumerator.Reset();
                    }
                }
            }
        }

        public class WrapperCollection<T> : ICollection<T>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IEnumerable<T> m_poEnumerable = null;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private int? m_iCount = null;

            public WrapperCollection(IEnumerable<T> s)
            {
                this.m_poEnumerable = s ?? throw new ArgumentNullException(nameof(s));
            }

            public virtual IEnumerable<T> Owner
            {
                get
                {
                    return m_poEnumerable;
                }
            }

            public virtual int Count
            {
                get
                {
                    ICollection<T> c = this.Owner as ICollection<T>;
                    if (c != null)
                    {
                        return c.Count;
                    }
                    else
                    {
                        lock (this)
                        {
                            if (m_iCount == null)
                            {
                                m_iCount = this.Owner.Count();
                            }
                        }
                        return m_iCount.Value;
                    }
                }
            }

            public virtual bool IsReadOnly
            {
                get
                {
                    ICollection<T> c = this.Owner as ICollection<T>;
                    if (c != null)
                    {
                        return c.IsReadOnly;
                    }
                    return true;
                }
            }

            public virtual void Add(T item)
            {
                ICollection<T> c = this.Owner as ICollection<T>;
                if (c == null)
                {
                    throw new NotImplementedException();
                }
                c.Add(item);
            }

            public virtual void Clear()
            {
                ICollection<T> c = this.Owner as ICollection<T>;
                if (c == null)
                {
                    throw new NotImplementedException();
                }
                c.Clear();
            }

            public virtual bool Contains(T item)
            {
                ICollection<T> c = this.Owner as ICollection<T>;
                if (c == null)
                {
                    throw new NotImplementedException();
                }
                return c.Contains(item);
            }

            public virtual void CopyTo(T[] array, int arrayIndex)
            {
                ICollection<T> c = this.Owner as ICollection<T>;
                if (c == null)
                {
                    throw new NotImplementedException();
                }
                c.CopyTo(array, arrayIndex);
            }

            public virtual IEnumerator<T> GetEnumerator()
            {
                return this.Owner.GetEnumerator();
            }

            public virtual bool Remove(T item)
            {
                ICollection<T> c = this.Owner as ICollection<T>;
                if (c == null)
                {
                    throw new NotImplementedException();
                }
                return c.Remove(item);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.Owner.GetEnumerator();
            }
        }

        public static ICollection<T> Collection<T>(this IEnumerable<T> s)
        {
            if (s == null)
                return null;
            return new WrapperCollection<T>(s);
        }

        public static IEnumerable<TResult> Conversion<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> conversion)
        {
            if (enumerable == null)
                return null;
            if (conversion == null)
                return null;
            return new WrapperEnumerable<T, TResult>(enumerable, conversion);
        }

        public class UnionEnumerable<T> : IEnumerable<T>
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IEnumerable<IEnumerable<T>>[] m_aoEnumerable;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IEnumerable<IEnumerable<T>> m_poFirstRootPtr;

            public UnionEnumerable(IEnumerable<IEnumerable<T>> s, params IEnumerable<IEnumerable<T>>[] enumerables)
            {
                this.m_poFirstRootPtr = s;
                this.m_aoEnumerable = enumerables;
            }

            public virtual IEnumerator<T> GetEnumerator()
            {
                if (this.m_poFirstRootPtr != null)
                {
                    foreach (var a in this.m_poFirstRootPtr)
                    {
                        if (a == null)
                        {
                            continue;
                        }
                        foreach (var b in a)
                        {
                            yield return b;
                        }
                    }
                }
                if (this.m_aoEnumerable != null)
                {
                    foreach (IEnumerable<IEnumerable<T>> a in this.m_aoEnumerable)
                    {
                        if (a == null)
                        {
                            continue;
                        }
                        foreach (var b in a)
                        {
                            if (b == null)
                            {
                                continue;
                            }
                            foreach (var c in b)
                            {
                                yield return c;
                            }
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public static IEnumerable<T> Union<T>(this IEnumerable<T> s, params IEnumerable<T>[] args)
        {
            return new UnionEnumerable<T>(new[] { s }, args);
        }

        public static IEnumerable<T> Union<T>(this IEnumerable<IEnumerable<T>> s, params IEnumerable<IEnumerable<T>>[] args)
        {
            return new UnionEnumerable<T>(s, args);
        }

        public class FilterEnumerable<T> : IEnumerable<T>
        {
            public FilterEnumerable(IEnumerable<T> s, Predicate<T> predicate)
            {
                this.Owner = s;
                this.Predicate = predicate;
            }

            public virtual Predicate<T> Predicate { get; private set; }

            public virtual IEnumerable<T> Owner { get; private set; }

            public virtual IEnumerator<T> GetEnumerator()
            {
                var s = this.Owner;
                if (s != null)
                {
                    var p = this.Predicate;
                    foreach (var o in s)
                    {
                        if (p == null || p(o))
                        {
                            yield return o;
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> s, Predicate<T> predicate)
        {
            if (s == null)
                return null;
            return new FilterEnumerable<T>(s, predicate);
        }
    }
}