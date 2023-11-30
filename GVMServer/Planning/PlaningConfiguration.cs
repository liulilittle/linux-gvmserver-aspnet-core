namespace GVMServer.Planning
{
    using System.IO;
    using System.Collections.Generic;
    using System.Reflection;
    using GVMServer.Planning.PlanningXml;
    using System;

    public static class PlaningConfiguration
    {
        private readonly static object syncobj = new object();

        public static void AddInclude(IEnumerable<string> includes)
        {
            DataModuleMgr.Instance.AddSearchPaths(includes, (path) => Directory.Exists(path));
        }

        public static void AddInclude(params string[] includes) => AddInclude((IEnumerable<string>)includes);

        public static IList<T> GetAll<T>() where T : class
        {
            if (!DataModuleMgr.Instance.TryGetConfigAll(out IList<T> s))
            {
                return null;
            }
            return s ?? new List<T>();
        }

        public static void Release()
        {
            DataModuleMgr.Instance.Clear(true);
        }

        public static void Clear()
        {
            DataModuleMgr.Instance.Clear(false);
        }

        public static IEnumerable<Type> GetAllExportedTypes()
        {
            return DataModuleMgr.Instance.GetAllExportedTypes();
        }

        public static void LoadAll(Assembly assembly)
        {
            DataModuleMgr.Instance.LoadAll(assembly);
        }

        public static void ReadAll()
        {
            DataModuleMgr.Instance.ReadAll();
        }

        public static void AddStdafx(params string[] stdafxs) => AddStdafx((IEnumerable<string>)stdafxs);

        public static void AddStdafx(IEnumerable<string> stdafxs)
        {
            DataModuleMgr.Instance.AddSearchPaths(stdafxs, (path) => File.Exists(path));
        }
    }
}
