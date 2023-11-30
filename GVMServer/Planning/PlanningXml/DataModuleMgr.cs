namespace GVMServer.Planning.PlanningXml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using GVMServer.Linq;
    using System.Collections.Concurrent;

    class DataModuleMgr
    {
        private static DataModuleMgr _instance;

        public static DataModuleMgr Instance
        {
            get
            {
                if (null == _instance)
                    _instance = new DataModuleMgr();
                return _instance;
            }
        }

        internal Dictionary<string, uint> _macros = new Dictionary<string, uint>();
        internal Dictionary<string, ComplexDataType> ComplexDataTypes = new Dictionary<string, ComplexDataType>();
        public Dictionary<string, DataModule> ConfigModules = new Dictionary<string, DataModule>();
        private XmlHandler _xmlHandler = new XmlHandler();
        private HashSet<string> _searchPaths = new HashSet<string>();
        private Dictionary<string, string> _configurationKeys = new Dictionary<string, string>();
        private Dictionary<string, string> _configurationValues = new Dictionary<string, string>();
        private ConcurrentDictionary<Type, Type> _configurationTypes = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// 添加搜索路径
        /// </summary>
        /// <param name="searchPaths"></param>
        public void AddSearchPaths(IEnumerable<string> searchPaths, Predicate<string> predicate)
        {
            if (searchPaths.IsNullOrEmpty() || searchPaths.IsNullOrEmpty())
            {
                return;
            }
            lock (this)
            {
                foreach (string i in searchPaths)
                {
                    var path = (i ?? string.Empty).TrimStart().TrimEnd();
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                    path = Path.GetFullPath(path);
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                    if (predicate == null || predicate(path))
                    {
                        _searchPaths.Add(path);
                    }
                }
            }
        }

        public void AddSearchPaths(Predicate<string> predicate, params string[] searchPaths) => AddSearchPaths((IEnumerable<string>)searchPaths, predicate);

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="essentialFiles">必须先处理的文件，包括keywords, common, rescommon</param>
        public void Init(IEnumerable<string> essentialFiles)
        {
            lock (this)
            {
                Dictionary<string, string> targetFiles = _configurationKeys;
                Dictionary<string, string> targetXmlFiles = _configurationValues;
                if (targetFiles.IsNullOrEmpty() || 
                    targetXmlFiles.IsNullOrEmpty() || 
                    targetFiles.Count != targetXmlFiles.Count)
                {
                    return;
                }

                HashSet<string> structFiles = new HashSet<string>();
                foreach (KeyValuePair<string, string> kv in targetFiles)
                {
                    string structFile = (kv.Key ?? string.Empty).TrimStart().TrimEnd();
                    if (string.IsNullOrEmpty(structFile))
                    {
                        continue;
                    }

                    structFiles.Add(structFile);
                }

                if (essentialFiles != null)
                {
                    foreach (string s in essentialFiles)
                    {
                        string structFile = (s ?? string.Empty).TrimStart().TrimEnd();
                        if (string.IsNullOrEmpty(structFile))
                        {
                            continue;
                        }
                        structFile = Path.GetFullPath(structFile);
                        if (string.IsNullOrEmpty(structFile))
                        {
                            continue;
                        }
                        structFiles.Add(structFile);
                    }
                }

                List<string> preHandleFiles = new List<string>();
                foreach (string search in _searchPaths)
                {
                    if (!Directory.Exists(search))
                    {
                        if (File.Exists(search))
                        {
                            string fileName = Path.GetFullPath(search);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                preHandleFiles.Add(fileName);
                            }
                        }
                        continue;
                    }
                    try
                    {
                        DirectoryInfo folder = new DirectoryInfo(search);
                        foreach (FileInfo file in folder.GetFiles())
                        {
                            if (structFiles.Contains(file.Name))
                            {
                                preHandleFiles.Add(file.FullName);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debugger.LogErr(e.ToString());
                    }
                }

                _xmlHandler.InitComplexDataStructs(preHandleFiles);

                foreach (string search in _searchPaths)
                {
                    try
                    {
                        if (!Directory.Exists(search))
                        {
                            continue;
                        }
                        DirectoryInfo folder = new DirectoryInfo(search);
                        foreach (FileInfo file in folder.GetFiles())
                        {
                            if (targetXmlFiles.TryGetValue(file.Name, out string value))
                            {
                                DataModule m = _xmlHandler.CreateModule(file.FullName);
                                if (m != null)
                                {
                                    ConfigModules.Add(value, m);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debugger.LogErr(e.ToString());
                    }
                }
            }
        }

        public void ReadAll(params string[] essentialFiles) => Init((IEnumerable<string>)essentialFiles);

        public IEnumerable<Type> GetAllExportedTypes() => _configurationTypes.Keys;

        public void LoadAll(Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }
            lock (this)
            {
                foreach (Type t in assembly.GetExportedTypes())
                {
                    foreach (Attribute a in t.GetCustomAttributes(true))
                    {
                        if (a is XmlFileNameAttribute)
                        {
                            KeyFileStruct kf = ((XmlFileNameAttribute)a).Key;
                            _configurationKeys.TryAdd(kf.Key, kf.FileName);
                            _configurationValues.TryAdd(kf.FileName, kf.Key);
                            _configurationTypes.TryAdd(t, t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取一个配置表模型数据
        /// </summary>
        /// <typeparam name="T">模型定义</typeparam>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool TryGetConfigAll<T>(out IList<T> ret) where T : class
        {
            ret = null;
            XmlFileNameAttribute kf = typeof(T).GetCustomAttributes(false).FirstOrDefault(o => o is XmlFileNameAttribute) as XmlFileNameAttribute;
            if (kf == null)
            {
                return false;
            }
            DataModule m = null;
            lock (this)
            {
                ConfigModules.TryGetValue(kf.Key.Key, out m);
            }
            if (m == null)
            {
                return false;
            }
            else
            {
                ret = m.GetAll<T>();
            }
            return true;
        }

        /// <summary>
        /// 使用完之后，清除掉内存的引用
        /// </summary>
        public void Clear(bool thoroughly = true)
        {
            lock (this)
            {
                if (thoroughly)
                {
                    this._macros.Clear();
                    this._configurationKeys.Clear();
                    this._configurationValues.Clear();
                    this._searchPaths.Clear();
                    this._configurationTypes.Clear();
                }

                foreach (KeyValuePair<string, ComplexDataType> pair in ComplexDataTypes)
                {
                    ComplexDataType c = pair.Value;
                    ComplexDataType.Release(c);
                }
                ComplexDataTypes.Clear();

                foreach (KeyValuePair<string, DataModule> pair in ConfigModules)
                {
                    DataModule m = pair.Value;
                    DataModule.Release(m);
                }
                ConfigModules.Clear();

                DataModule.ClearPool();
                ComplexDataType.ClearPool();
            }
        }
    }
}
