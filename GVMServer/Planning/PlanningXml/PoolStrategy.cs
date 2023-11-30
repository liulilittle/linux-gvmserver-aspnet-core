namespace GVMServer.Planning.PlanningXml
{
    using System.Collections.Generic;

    /// <summary>
    /// 池策略的静态存取接口，替代new
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class PoolGetRelease<T> where T : IPoolObject, new()
    {
        public static DataPool<T> SelfPool = new DataPool<T>();

        public static T Get()
        {
            return SelfPool.Get();
        }

        public static void Release(T this_)
        {
            SelfPool.Release(this_);
        }

        public static void ClearPool()
        {
            SelfPool.ClearUnused();
        }
    }

    /// <summary>
    /// 池策略的重置接口
    /// </summary>
    interface IPoolObject
    {
        void Init();

        void Reset();
    }

    class DataPool<T> where T : IPoolObject, new()
    {
        private Stack<T> _container = new Stack<T>();

        public int CountAll { get; set; }

        public int CountActive { get { return CountAll - CountInactive; } }

        public int CountInactive { get { return _container.Count; } }


        public T Get()
        {
            T element;
            if (_container.Count == 0)
            {
                element = new T();
                element.Init();
                CountAll++;
            }
            else
            {
                element = _container.Pop();
            }
            return element;
        }

        public void Release(T element)
        {
            element.Reset();
            _container.Push(element);
        }

        public void ClearUnused()
        {
            CountAll -= _container.Count;
            _container.Clear();
        }
    }
}
