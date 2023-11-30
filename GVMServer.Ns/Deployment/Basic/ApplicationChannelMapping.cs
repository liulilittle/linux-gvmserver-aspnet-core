namespace GVMServer.Ns.Deployment.Basic
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using GVMServer.Ns.Net;
    using GVMServer.W3Xiyou.Net;

    public class ApplicationChannelMapping : IEnumerable<ApplicationType>
    {
        private readonly ConcurrentDictionary<ApplicationType, ApplicationChannelMappingObject> m_oMappingObject 
            = new ConcurrentDictionary<ApplicationType, ApplicationChannelMappingObject>();

        public class ApplicationChannelMappingObject
        {
            private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, ISocket>> m_poGSMappingTable
                = new ConcurrentDictionary<string, ConcurrentDictionary<int, ISocket>>();

            private static string PLATFORM_TEXT(string platform)
            {
                platform = (platform ?? string.Empty).TrimStart().TrimEnd();
                if (!string.IsNullOrEmpty(platform))
                {
                    platform = XiYouSdkClient.DEFAULT_PLATFORM;
                }
                return platform;
            }

            public virtual bool Add(string platform, int sid, ISocket socket)
            {
                if (socket == null || 0 == sid)
                {
                    return false;
                }

                platform = PLATFORM_TEXT(platform);
                lock (m_poGSMappingTable)
                {
                    if (!m_poGSMappingTable.TryGetValue(platform, out ConcurrentDictionary<int, ISocket> poServerTable))
                    {
                        poServerTable = new ConcurrentDictionary<int, ISocket>();
                        m_poGSMappingTable.TryAdd(platform, poServerTable);
                    }

                    if (!poServerTable.TryGetValue(sid, out ISocket goSocket) || goSocket == null)
                    {
                        poServerTable[sid] = socket;
                    }
                }
                return true;
            }

            public virtual ISocket Remove(string platform, int sid)
            {
                platform = PLATFORM_TEXT(platform);
                lock (m_poGSMappingTable)
                {
                    if (!m_poGSMappingTable.TryGetValue(platform, out ConcurrentDictionary<int, ISocket> poServerTable))
                    {
                        return null;
                    }

                    poServerTable.TryRemove(sid, out ISocket socket);
                    return socket;
                }
            }

            public virtual bool RemoveAll(string platform)
            {
                platform = PLATFORM_TEXT(platform);
                lock (m_poGSMappingTable)
                {
                    return m_poGSMappingTable.TryRemove(platform, out ConcurrentDictionary<int, ISocket> poServerTable);
                }
            }

            public virtual void RemoveAll()
            {
                lock (m_poGSMappingTable)
                {
                    m_poGSMappingTable.Clear();
                }
            }

            public virtual IEnumerable<string> GetAllPlatformNames()
            {
                return m_poGSMappingTable.Keys;
            }

            public virtual IEnumerable<KeyValuePair<int, ISocket>> GetAllSockets(string platform)
            {
                platform = PLATFORM_TEXT(platform);
                lock (m_poGSMappingTable)
                {
                    if (!m_poGSMappingTable.TryGetValue(platform, out ConcurrentDictionary<int, ISocket> poServerTable))
                    {
                        return null;
                    }

                    return poServerTable;
                }
            }

            public virtual ISocket GetChannel(string platform, int sid)
            {
                platform = PLATFORM_TEXT(platform);
                lock (m_poGSMappingTable)
                {
                    if (!m_poGSMappingTable.TryGetValue(platform, out ConcurrentDictionary<int, ISocket> poServerTable))
                    {
                        return null;
                    }

                    if (poServerTable.TryGetValue(sid, out ISocket socket))
                    {
                        return socket;
                    }
                }

                return null;
            }
        }

        public virtual ApplicationChannelMappingObject GetMappingObject(ApplicationType applicationType)
        {
            lock (this.m_oMappingObject)
            {
                if (!this.m_oMappingObject.TryGetValue(applicationType, out ApplicationChannelMappingObject poApplicationChannelMappingObject) 
                    || poApplicationChannelMappingObject == null)
                {
                    this.m_oMappingObject[applicationType] = poApplicationChannelMappingObject = new ApplicationChannelMappingObject();
                }
                return poApplicationChannelMappingObject;
            }
        }

        public virtual IEnumerator<ApplicationType> GetEnumerator()
        {
            return this.m_oMappingObject.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
