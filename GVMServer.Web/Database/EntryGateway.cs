namespace GVMServer.Web.Database
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using GVMServer.W3Xiyou.Docking;
    using GVMServer.Web.Database.Basic;
    using GVMServer.Web.Model;
    using GVMServer.Web.Model.Enum;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using IDataAdapter = GVMServer.Web.Database.Basic.IDataAdapter;

    public class EntryGateway : IEntryGateway
    {
        private DataNode[] m_DateNodes = null;
        private readonly IList<object> m_EmptyArray = new List<object>();

        public EntryGateway()
        {
            this.m_DateNodes = Startup.GetDefaultConfiguration().
                GetSection("Database").
                GetSection("MySql").
                GetSection("DataNodes").Get<DataNode[]>();
        }

        public object FindCurrentPackageInfo(string platform, string paid)
        {
            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return null;
            }

            DataTableGateway gateway = new DataTableGateway(adapter);
            try
            {
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(paid) || paid == "0")
                        {
                            command.CommandText = "SELECT * FROM package_info WHERE enable=1 AND platform=@szPlatform AND (paid IS NULL OR paid='' OR paid='0') LIMIT 1;";
                        }
                        else
                        {
                            command.CommandText = "SELECT * FROM package_info WHERE enable=1 AND platform=@szPlatform AND paid=@szPAID LIMIT 1;";
                        }

                        var parameter = adapter.CreateParameter();
                        parameter.ParameterName = "@szPlatform";
                        parameter.Value = platform ?? string.Empty;
                        command.Parameters.Add(parameter);

                        if (!string.IsNullOrEmpty(paid))
                        {
                            parameter = adapter.CreateParameter();
                            parameter.ParameterName = "@szPAID";
                            parameter.Value = paid ?? string.Empty;
                            command.Parameters.Add(parameter);
                        }

                        return (gateway.Select<object>(command) ?? m_EmptyArray).FirstOrDefault();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object FindCurrentSettings(string platform, string paid)
        {
            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return null;
            }

            DataTableGateway gateway = new DataTableGateway(adapter);
            try
            {
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(paid) || paid == "0")
                        {
                            command.CommandText = "SELECT * FROM settings_info WHERE enable=1 AND platform=@szPlatform AND (paid IS NULL OR paid='' OR paid='0') LIMIT 1;";
                        }
                        else
                        {
                            command.CommandText = "SELECT * FROM settings_info WHERE enable=1 AND platform=@szPlatform AND paid=@szPAID LIMIT 1;";
                        }

                        var parameter = adapter.CreateParameter();
                        parameter.ParameterName = "@szPlatform";
                        parameter.Value = platform ?? string.Empty;
                        command.Parameters.Add(parameter);

                        if (!string.IsNullOrEmpty(paid))
                        {
                            parameter = adapter.CreateParameter();
                            parameter.ParameterName = "@szPAID";
                            parameter.Value = paid ?? string.Empty;
                            command.Parameters.Add(parameter);
                        }

                        return (gateway.Select<object>(command) ?? m_EmptyArray).FirstOrDefault();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IList<object> FindAllServers(string platform, string paid)
        {
            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return null;
            }

            try
            {
                DataTableGateway gateway = new DataTableGateway(adapter);
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(paid) || paid == "0")
                        {
                            command.CommandText = "SELECT * FROM servers_info WHERE platform=@szPlatform AND (paid IS NULL OR paid='' OR paid='0') AND group_name IS NOT NULL AND group_name != '';";
                        }
                        else
                        {
                            command.CommandText = "SELECT * FROM servers_info WHERE platform=@szPlatform AND paid=@szPAID AND group_name IS NOT NULL AND group_name != '';";
                        }

                        var parameter = adapter.CreateParameter();
                        parameter.ParameterName = "@szPlatform";
                        parameter.Value = platform ?? string.Empty;
                        command.Parameters.Add(parameter);

                        if (!string.IsNullOrEmpty(paid))
                        {
                            parameter = adapter.CreateParameter();
                            parameter.ParameterName = "@szPAID";
                            parameter.Value = paid ?? string.Empty;
                            command.Parameters.Add(parameter);
                        }

                        return gateway.Select<object>(command) ?? m_EmptyArray;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object FindCurrentSwitchList(string platform, string paid)
        {
            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return null;
            }

            try
            {
                DataTableGateway gateway = new DataTableGateway(adapter);
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(paid) || paid == "0")
                        {
                            command.CommandText = "SELECT * FROM switch_list WHERE enable=1 AND (paid IS NULL OR paid='' OR paid='0') AND platform=@szPlatform LIMIT 1;";
                        }
                        else
                        {
                            command.CommandText = "SELECT * FROM switch_list WHERE enable=1 AND paid=@szPAID AND platform=@szPlatform LIMIT 1;";
                        }

                        var parameter = adapter.CreateParameter();
                        parameter.ParameterName = "@szPlatform";
                        parameter.Value = platform ?? string.Empty;
                        command.Parameters.Add(parameter);

                        if (!string.IsNullOrEmpty(paid))
                        {
                            parameter = adapter.CreateParameter();
                            parameter.ParameterName = "@szPAID";
                            parameter.Value = paid ?? string.Empty;
                            command.Parameters.Add(parameter);
                        }

                        return (gateway.Select<object>(command) ?? m_EmptyArray).FirstOrDefault();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool GetBooleanValue(JObject o, string key)
        {
            if (o == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!o.TryGetValue(key, out JToken t))
            {
                return false;
            }

            string s = t.ToString();
            if (s == "True" || s == "true" || s == "1")
            {
                return true;
            }

            return false;
        }

        private JObject ParseSwitchList(JObject o)
        {
            if (o != null)
            {
                var aszSwitchListKeys = Startup.GetDefaultConfiguration().
                    GetSection("Business").
                    GetSection("Service").
                    GetSection("Entities").
                    GetSection("SwitchListBooleanKeys").Get<string[]>();
                if (aszSwitchListKeys != null)
                {
                    foreach (string szKey in aszSwitchListKeys)
                    {
                        o[szKey] = GetBooleanValue(o, szKey);
                    }
                }
            }
            return o;
        }

        private JObject CreateEntry(IList<dynamic> servers, dynamic settings, dynamic packageinfo, dynamic switchlist)
        {
            JObject owner = new JObject();
            owner["param_list"] = JObject.FromObject(settings);
            owner["param_list"]["switch_list"] = ParseSwitchList(JObject.FromObject(switchlist));

            JObject o = (JObject)owner["param_list"]["switch_list"];
            o.Remove("enable");
            o.Remove("platform");

            o = (JObject)owner["param_list"];
            o.Remove("enable");
            o["update_package"] = GetBooleanValue(o, "update_package");

            IEnumerable servergourpus = null;
            if (servers == null || servers.Count <= 0)
            {
                servergourpus = m_EmptyArray;
            }
            else
            {
                servergourpus = from server in servers
                                orderby server.sid ascending
                                group server by new
                                {
                                    //cs_address = server.cs_address,
                                    //cs_port = server.cs_port,
                                    group_name = Convert.ToString(server.group_name ?? string.Empty),
                                }
                                    into chatsvrgourps
                                orderby chatsvrgourps.Key.group_name ascending
                                select new
                                {
                                    group_name = chatsvrgourps.Key.group_name,
                                    group_list = new
                                    {
                                        //chatSvr = new
                                        //{
                                        //    ip = chatsvrgourps.Key.cs_address,
                                        //    port = chatsvrgourps.Key.cs_port,
                                        //},
                                        gameSvr = chatsvrgourps.Select((server) =>
                                        {
                                            IDictionary<string, object> eo = new ExpandoObject();
                                            (server as object).GetType().GetProperties().FirstOrDefault(p1 =>
                                            {
                                                string key = p1.Name;
                                                switch (key)
                                                {
                                                    case "areaid":
                                                        key = "id";
                                                        break;
                                                    case "gs_address":
                                                        key = "ip";
                                                        break;
                                                    case "gs_name":
                                                        key = "name";
                                                        break;
                                                    case "gs_port":
                                                        key = "port";
                                                        break;
                                                }
                                                if (eo.ContainsKey(key))
                                                {
                                                    eo[key] = p1.GetValue(server);
                                                }
                                                else
                                                {
                                                    eo.Add(key, p1.GetValue(server));
                                                }
                                                return false;
                                            });
                                            return eo;
                                        })
                                    }
                                };
            }
            owner["server_info"] = new JObject();
            owner["server_info"]["last_server"] = 1;
            owner["server_info"]["server_time"] = XiYouUtility.ToTimeSpan10(DateTime.Now);

            owner["server_info"]["server_list"] = new JObject();
            o = (JObject)owner["server_info"]["server_list"];

            if (servergourpus != null)
            {
                foreach (dynamic servergourpu in servergourpus)
                {
                    o[servergourpu.group_name] = JObject.FromObject(servergourpu.group_list);
                }
            }

            owner["version_info"] = new JObject();
            owner["version_info"]["package_info"] = JObject.FromObject(packageinfo);
            owner["version_info"]["update_data"] = packageinfo.update_data;

            string asserts_info = Convert.ToString(packageinfo.assets_info ?? string.Empty);
            string version_hash = Convert.ToString(packageinfo.version_hash ?? string.Empty);
            JToken token = null;
            if (!string.IsNullOrEmpty(asserts_info))
            {
                try { token = JToken.Parse(asserts_info); } catch (Exception) { }
            }
            if (token == null)
            {
                token = new JObject();
                token["version_hash"] = version_hash;
            }
            owner["version_info"]["assets_info"] = token;
            owner["version_info"]["version_hash"] = version_hash;

            o = (JObject)owner["version_info"];
            o["version_hash"] = GetBooleanValue(o, "version_hash");

            o = (JObject)owner["version_info"]["package_info"];
            o.Remove("assets_info");
            o.Remove("update_data");
            o.Remove("version_hash");
            o.Remove("enable");
            o.Remove("platform");

            return owner;
        }

        private JObject CreateServer(dynamic server, dynamic settings)
        {
            JObject owner = new JObject();

            owner["ServerId"] = server.sid;
            owner["AreaId"] = server.areaid;
            owner["Platform"] = server.platform;
            owner["Name"] = server.gs_name;

            JObject jo = new JObject();
            jo["Address"] = server.cs_address;
            jo["Port"] = server.cs_port;
            owner["ChatServer"] = jo;

            jo = new JObject();
            jo["Address"] = server.gs_address;
            jo["Port"] = server.gs_port;
            owner["GameServer"] = jo;

            owner["UpdateUri"] = settings.update_url;
            owner["UpdateUri2"] = settings.update_url2;

            return owner;
        }

        private JObject CreateServerDictionary(IList<dynamic> servers, dynamic settings)
        {
            JObject roots = new JObject();
            foreach (dynamic o in servers)
            {
                string sid = Convert.ToString(o.sid);
                roots[sid] = CreateServer(o, settings);
            }
            return roots;
        }

        private JArray ConvertServerDictionaryToServerArray(JObject servers)
        {
            JArray s = new JArray();
            foreach (var kv in servers)
            {
                JObject value = (JObject)kv.Value;
                s.Add(value);
            }
            return s;
        }

        private class RefreshPaidContent
        {
            public dynamic settings;
            public dynamic packageinfo;
            public dynamic switchlist;
            public IList<object> servers;
        }

        public RefreshEntities Refresh(string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return null;
            }

            RefreshEntities entities = new RefreshEntities();
            entities.Platform = platform;

            IDictionary<string, RefreshPaidContent> paidcontents = new Dictionary<string, RefreshPaidContent>();
            foreach (var paid in FindAllPaid(platform))
            {
                try
                {
                    RefreshPaidContent paidowner = null;
                    paidcontents.TryGetValue(string.Empty, out paidowner);

                    IList<object> servers = FindAllServers(platform, paid); // overlapped
                    if (servers == null || servers.Count <= 0)
                    {
                        servers = paidowner?.servers ?? m_EmptyArray;
                    }

                    if (servers != null && servers != paidowner?.servers)
                    {
                        var paidownerservers = paidowner?.servers ?? m_EmptyArray;
                        foreach (dynamic server in paidownerservers)
                        {
                            if (server == null)
                            {
                                continue;
                            }

                            if (servers.FirstOrDefault((dynamic i) => i?.sid == server.sid) == null)
                            {
                                servers.Add(server);
                            }
                        }
                    }

                    dynamic settings = FindCurrentSettings(platform, paid) ?? paidowner?.settings;
                    dynamic packageinfo = FindCurrentPackageInfo(platform, paid) ?? paidowner?.packageinfo;
                    dynamic switchlist = FindCurrentSwitchList(platform, paid) ?? paidowner?.switchlist;

                    if (settings == null || packageinfo == null || switchlist == null)
                    {
                        continue;
                    }

                    JObject entryinfo = CreateEntry(servers, settings, packageinfo, switchlist);
                    JObject serverdictionary = CreateServerDictionary(servers, settings);
                    JArray serverlist = ConvertServerDictionaryToServerArray(serverdictionary);

                    paidcontents.Add(paid, new RefreshPaidContent()
                    {
                        settings = settings,
                        packageinfo = packageinfo,
                        switchlist = switchlist,
                        servers = servers
                    });
                    entities.Entities.Add(paid, new RefreshEntities.Entry()
                    {
                        EntryInfo = entryinfo,
                        Paid = paid,
                        ServerList = serverlist,
                        Servers = serverdictionary
                    });
                }
                catch (Exception)
                {

                }
            }

            return entities;
        }

        public DataNode GetAvailableNode(out IDataAdapter adapter)
        {
            adapter = null;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds <= 3000)
            {
                foreach (DataNode node in m_DateNodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    if (node.Master.Available)
                    {
                        adapter = node.Master;
                        return node;
                    }

                    if (node.Salve.Available)
                    {
                        adapter = node.Salve;
                        return node;
                    }
                }

                Thread.Sleep(100);
            }

            stopwatch.Stop();
            return null;
        }

        public IList<string> FindAllPaid(string platform)
        {
            ISet<string> sets = new HashSet<string>();
            IList<string> results = new List<string>();
            results.Add(string.Empty);
            sets.Add(string.Empty);

            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null || string.IsNullOrEmpty(platform))
            {
                return results;
            }

            try
            {
                DataTableGateway gateway = new DataTableGateway(adapter);
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        command.CommandText = @"SELECT DISTINCT * FROM (SELECT paid FROM servers_info WHERE platform=@szPlatform GROUP BY paid
                                                UNION ALL
                                                SELECT paid FROM package_info WHERE platform=@szPlatform GROUP BY paid
                                                UNION ALL
                                                SELECT paid FROM settings_info WHERE platform=@szPlatform GROUP BY paid
                                                UNION ALL
                                                SELECT paid FROM switch_list WHERE platform=@szPlatform GROUP BY paid) AS APAID_CTE";

                        var parameter = adapter.CreateParameter();
                        parameter.ParameterName = "@szPlatform";
                        parameter.Value = platform ?? string.Empty;
                        command.Parameters.Add(parameter);

                        IList<object> s = gateway.Select<object>(command) ?? m_EmptyArray;
                        foreach (dynamic o in s)
                        {
                            bool add = false;
                            string paid = string.Empty;
                            if (o.paid == null || DBNull.Value.Equals(o.paid))
                            {
                                add |= sets.Add(string.Empty);
                            }
                            else
                            {
                                paid = o.paid.ToString() ?? string.Empty;
                                paid = paid.TrimStart().TrimEnd();
                                if (paid == "0")
                                {
                                    paid = string.Empty;
                                }
                                add |= sets.Add(paid);
                            }
                            if (!add)
                            {
                                continue;
                            }
                            if (!string.IsNullOrEmpty(paid))
                            {
                                results.Add(paid);
                            }
                            else
                            {
                                results.Insert(0, paid);
                            }
                        }

                        return results;
                    }
                    catch (Exception)
                    {
                        return results;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IList<string> GetAllPlatform()
        {
            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return null;
            }

            try
            {
                DataTableGateway gateway = new DataTableGateway(adapter);
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        command.CommandText = "SELECT platform FROM servers_info GROUP BY platform;";
                        IList<object> s = gateway.Select<object>(command);
                        IList<string> results = new List<string>();
                        if (s != null && s.Count > 0)
                        {
                            foreach (dynamic o in s)
                            {
                                string platform = Convert.ToString(o.platform ?? string.Empty);
                                if (string.IsNullOrEmpty(platform))
                                {
                                    continue;
                                }
                                results.Add(platform);
                            }
                        }
                        return results;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IDictionary<string, HashSet<string>> GetAllEntities()
        {
            IDictionary<string, HashSet<string>> results = new Dictionary<string, HashSet<string>>();

            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return results;
            }

            try
            {
                DataTableGateway gateway = new DataTableGateway(adapter);
                using (var command = adapter.CreateCommand())
                {
                    try
                    {
                        command.CommandText = "SELECT platform, paid FROM servers_info GROUP BY paid, platform;";
                        IList<dynamic> s = gateway.Select<object>(command);
                        if (s != null && s.Count > 0)
                        {
                            results = (from r in s
                                       orderby r.platform ascending
                                       group r by r.platform
                                     into ps
                                       orderby ps.Key ascending
                                       select ps).ToDictionary(a => a.Key as string, b => b.Select((o) =>
                                       {
                                           string paid = o.paid as string ?? string.Empty;
                                           paid = paid.TrimStart().TrimEnd();
                                           if (paid == "0")
                                           {
                                               paid = string.Empty;
                                           }
                                           return paid;
                                       }).ToHashSet());
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return results;
        }

        public virtual ServerAddError Add(ServerAddInfo model)
        {
            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return ServerAddError.ServerAddError_NoAvailableDatabaseNodeWasFound;
            }

            if (string.IsNullOrEmpty(model.platform))
            {
                return ServerAddError.ServerAddError_PlatformNotExists;
            }

            DbConnection connection = null;
            try
            {
                connection = adapter.GetConnection();
                if (connection != null)
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                    }
                }
            }
            catch (Exception)
            {

            }

            if (connection == null)
            {
                return ServerAddError.ServerAddError_TryToAccessDatabaseManyTimesButBecauseTheMachineActivelyRefused;
            }

            DbTransaction transaction = null;
            try
            {
                transaction = connection.BeginTransaction();
            }
            catch (Exception)
            {

            }

            if (transaction == null)
            {
                return ServerAddError.ServerAddError_UnableToPullUpTheDatabaseTransaction;
            }

            try
            {
                string paid = model.paid ?? string.Empty;
                if (paid == "0")
                {
                    paid = string.Empty;
                }
                using (transaction)
                {
                    bool rollback = false;
                    try
                    {
                        DbParameter PARAM(string key, object value)
                        {
                            var p = adapter.CreateParameter();
                            p.ParameterName = key ?? string.Empty;
                            p.Value = value;
                            return p;
                        }
                        do
                        {
                            DataTableGateway gateway = new DataTableGateway(adapter);
                            using (var command = adapter.CreateCommand())
                            {
                                command.Connection = connection;
                                command.CommandText = @"SELECT
                                    (SELECT COUNT(1) FROM switch_list WHERE switch_list.platform = @platform LIMIT 1) AS switch_list,
                                    (SELECT COUNT(1) FROM package_info WHERE package_info.platform = @platform LIMIT 1) AS package_info,
                                    (SELECT COUNT(1) FROM settings_info WHERE settings_info.platform = @platform LIMIT 1) AS settings_info";
                                command.Parameters.Add(PARAM("@platform", model.platform));
                                try
                                {
                                    using (DbDataReader dr = command.ExecuteReader())
                                    {
                                        int counter = 0;
                                        try
                                        {
                                            while (dr.Read())
                                            {
                                                for (int i = 0; i < dr.FieldCount; i++)
                                                {
                                                    int n = dr.GetInt32(i);
                                                    if (n > 0)
                                                    {
                                                        counter++;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        {

                                        }
                                        if (counter <= 0)
                                        {
                                            return ServerAddError.ServerAddError_PlatformNotExists;
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    return ServerAddError.ServerAddError_UnableToAccessExecuteReaderOperationsFromTheDatabase;
                                }
                            }

                            using (var command = adapter.CreateCommand())
                            {
                                command.Connection = connection;
                                command.CommandText = "DELETE FROM servers_info WHERE sid=@sid AND platform=@platform AND ";
                                {
                                    if (string.IsNullOrEmpty(paid))
                                    {
                                        command.CommandText += " (paid IS NULL OR paid='' OR paid='0')";
                                    }
                                    else
                                    {
                                        command.CommandText += " paid=@paid";
                                    }
                                    command.Parameters.Add(PARAM("@sid", model.sid));
                                    command.Parameters.Add(PARAM("@paid", paid));
                                    command.Parameters.Add(PARAM("@platform", model.platform));
                                }
                                try
                                {
                                    if (0 > command.ExecuteNonQuery())
                                    {
                                        rollback |= true;
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    rollback |= true;
                                    break;
                                }
                            }
                            using (var command = adapter.CreateCommand())
                            {
                                command.Connection = connection;
                                command.CommandText =
                                    @"INSERT INTO servers_info(sid, areaid, gs_address, gs_port, platform, cs_address, cs_port, gs_name, battle, group_name, paid) 
                                    VALUE(@sid, @areaid, @gs_address, @gs_port, @platform, @cs_address, @cs_port, @gs_name, @battle, @group_name, @paid)";
                                {
                                    command.Parameters.Add(PARAM("@sid", model.sid));
                                    command.Parameters.Add(PARAM("@areaid", model.aid));
                                    command.Parameters.Add(PARAM("@gs_address", model.gamesvr_address));
                                    command.Parameters.Add(PARAM("@gs_port", model.gamesvr_port));
                                    command.Parameters.Add(PARAM("@platform", model.platform));
                                    command.Parameters.Add(PARAM("@cs_address", model.chatsvr_address));
                                    command.Parameters.Add(PARAM("@cs_port", model.chatsvr_port));
                                    command.Parameters.Add(PARAM("@gs_name", model.server_name));
                                    command.Parameters.Add(PARAM("@battle", 0));
                                    command.Parameters.Add(PARAM("@group_name", model.group_name));
                                    command.Parameters.Add(PARAM("@paid", paid));
                                }
                                try
                                {
                                    if (0 >= command.ExecuteNonQuery())
                                    {
                                        rollback |= true;
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    rollback |= true;
                                    break;
                                }
                            }
                        } while (false);
                    }
                    catch (Exception)
                    {
                        rollback |= true;
                    }

                    if (!rollback)
                    {
                        try
                        {
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            return ServerAddError.ServerAddError_TheDatabaseTransactionInstanceCouldNotBeCommit;
                        }
                    }
                    else
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception)
                        {
                            return ServerAddError.ServerAddError_TheDatabaseTransactionInstanceCouldNotBeRollback;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return ServerAddError.ServerAddError_TheDatabaseTransactionInstanceCouldNotBeRelease;
            }

            return ServerAddError.ServerAddError_Success;
        }
    }
}
