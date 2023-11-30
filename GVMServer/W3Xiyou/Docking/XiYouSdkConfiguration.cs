namespace GVMServer.W3Xiyou.Docking
{
    using System;
    using GVMServer.Configuration;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using GVMServer.Valuetype;
    using GVMServer.Linq;
    using Microsoft.Extensions.Configuration;

    public class XiYouSdkConfiguration
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IList<XiYouSdkConfiguration> m_ConfigList = new List<XiYouSdkConfiguration>();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static IDictionary<string, XiYouSdkConfiguration> m_ConfigTable = new Dictionary<string, XiYouSdkConfiguration>();

        private const string XIYOUSDK_NAME = "xiyousdk";

        public string PackageName { get; set; }

        public string AppId { get; set; }

        public string AppKey { get; set; }

        public string AppSecret { get; set; }

        public bool SignToLower { get; set; }

        public static void LoadFrom(string path)
        {
            INIDocument document = new INIDocument();
            document.Load(path);
            LoadFrom(document);
        }

        public static void LoadFrom(INIDocument document)
        {
            IEnumerable<INISection> sections = document ?? throw new ArgumentNullException(nameof(document));
            foreach (INISection section in sections)
            {
                string sectionName = section.Name;
                if (sectionName.Length < XIYOUSDK_NAME.Length)
                {
                    continue;
                }
                if (XIYOUSDK_NAME == sectionName.Substring(0, XIYOUSDK_NAME.Length))
                {
                    XiYouSdkConfiguration conf = new XiYouSdkConfiguration();
                    foreach (PropertyInfo pi in typeof(XiYouSdkConfiguration).GetProperties())
                    {
                        INIKey kv = section.Values.FirstOrDefault(p => p.Key.ToLower() == pi.Name.ToLower());
                        if (kv != null)
                        {
                            object oValue = ValuetypeFormatter.Parse(kv.Value, pi.PropertyType);
                            if (oValue != null)
                            {
                                pi.SetValue(conf, oValue);
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(conf.AppId))
                    {
                        continue;
                    }
                    m_ConfigList.Add(conf);
                    m_ConfigTable.Add(conf.AppId, conf);
                }
            }
        }

        public static void LoadFrom( IConfiguration configuration )
        {
            XiYouSdkConfiguration[] configs = configuration.GetSection( "Sdk" ).GetSection( "Dockings" ).Get<XiYouSdkConfiguration[]>();
            foreach ( XiYouSdkConfiguration config in configs )
            {
                if ( config == null || string.IsNullOrEmpty( config.AppId ) )
                {
                    continue;
                }
                m_ConfigList.Add( config );
                m_ConfigTable.Add( config.AppId, config );
            }
        }

        public static XiYouSdkConfiguration GetConfiguration(int category)
        {
            if (category < 0 || category >= m_ConfigList.Count)
            {
                return null;
            }
            return m_ConfigList[category];
        }

        public static XiYouSdkConfiguration GetConfiguration(string appID)
        {
            m_ConfigTable.TryGetValue(appID, out XiYouSdkConfiguration configuration);
            return configuration;
        }

        public static IEnumerable<XiYouSdkConfiguration> GetAllConfiguration()
        {
            return m_ConfigList;
        }
    }
}
