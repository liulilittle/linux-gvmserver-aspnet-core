namespace GVMServer.Ns.Functional
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Threading;
    using GVMServer.DDD.Service;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Web.Database.Basic;
    using Microsoft.Extensions.Configuration;
    using IDataAdapter = GVMServer.Web.Database.Basic.IDataAdapter;

    public class Ns
    {
        public ApplicationType ApplicationType { get; set; }

        public string AddressMask { get; set; }

        public string PlatformName { get; set; }

        public int ServerNo { get; set; }

        public int ServerAreaId { get; set; }

        public Guid AssignNodeid { get; set; }

        public string INetAddress { get; set; }

        public DateTime CreationTime { get; set; }

        public byte BattleSuit { get; set; }
    }

    public class NsService : IServiceBase
    {
        private DataNode[] m_DateNodes = null;
        private readonly IList<Ns> m_EmptyArray = new List<Ns>();

        public NsService()
        {
            BaseApplication application = ServiceObjectContainer.Get<BaseApplication>();
            if (application == null)
            {
                this.m_DateNodes = new DataNode[0];
            }
            else
            {
                this.m_DateNodes = application.GetConfiguration().
                    GetSection("Database").
                    GetSection("MySql").
                    GetSection("DataNodes").Get<DataNode[]>();
            }
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

        private static DbParameter CreateParameter(IDataAdapter adapter, string key, object value)
        {
            DbParameter dbParameter = adapter.CreateParameter();
            dbParameter.ParameterName = key;
            dbParameter.Value = value;
            return dbParameter;
        }

        private Error InternalExecuteOrSelect(Action<IDataAdapter, DbCommand> callback, out DataTable dataTable, out int nonQuery, bool selectMode)
        {
            dataTable = null;
            nonQuery = -0;

            GetAvailableNode(out IDataAdapter adapter);
            if (adapter == null)
            {
                return Error.Error_TheAvailableDataAdapterCouldNotBeFound;
            }

            DbConnection connection = null;
            try
            {
                connection = adapter.GetConnection();
            }
            catch (Exception)
            {

            }

            if (connection == null)
            {
                return Error.Error_UnableToGetDatabaseConnection;
            }

            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
            }
            catch (Exception)
            {
                return Error.Error_UnableToOpenDatabaseConnection;
            }

            var command = adapter.CreateCommand();
            Error error = Error.Error_Success;
            try
            {
                if (command == null)
                {
                    return Error.Error_UnableToCreateDatabaseCommand;
                }
                try
                {
                    command.Connection = connection;
                    callback(adapter, command);
                    if (selectMode)
                    {
                        using (var da = adapter.CreateAdapter())
                        {
                            DataTable dtDataTemp = new DataTable();
                            try
                            {
                                da.SelectCommand = command;
                                da.Fill(dtDataTemp);

                                dataTable = dtDataTemp;
                            }
                            catch (Exception)
                            {
                                dtDataTemp.Dispose();
                                return Error.Error_TheQueryOperationCannotBePerformedAgainstTheDatabase;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            nonQuery = command.ExecuteNonQuery();
                        }
                        catch (Exception)
                        {
                            nonQuery = -1;
                        }
                    }
                }
                catch (Exception)
                {
                    error = Error.Error_UnknowTheDatabaseExecuteException;
                }

                command.Dispose();
            }
            catch (Exception)
            {

            }
            return error;
        }

        private Error InternalSelect(Action<IDataAdapter, DbCommand> callback, out DataTable dataTable)
        {
            return InternalExecuteOrSelect(callback, out dataTable, out int nonQeury, true);
        }

        private Error InternalExecuteNonQuery(Action<IDataAdapter, DbCommand> callback, out int nonquerying)
        {
            return InternalExecuteOrSelect(callback, out DataTable dt, out nonquerying, false);
        }

        private IList<Ns> ConvertToAllModel(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count <= 0)
            {
                return m_EmptyArray;
            }
            Func<DataRow, string, object> vt = (row, key) =>
            {
                object o = row[key];
                if (o == null || o == DBNull.Value)
                {
                    return null;
                }

                return o;
            };
            IList<Ns> models = new List<Ns>();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                DataRow row = dataTable.Rows[i];
                Ns model = new Ns()
                {
                    AddressMask = (vt(row, "address_mask")?.ToString() ?? string.Empty),
                    CreationTime = Convert.ToDateTime(vt(row, "creation_time") ?? DateTime.MinValue),
                    ServerNo = Convert.ToInt32(vt(row, "server_no") ?? 0),
                    ApplicationType = (ApplicationType)Convert.ToInt32(vt(row, "application_type") ?? 0),
                    INetAddress = (vt(row, "inet_address")?.ToString() ?? string.Empty),
                    PlatformName = (vt(row, "platform_name")?.ToString() ?? string.Empty),
                    ServerAreaId = Convert.ToInt32(vt(row, "serverarea_id") ?? 0),
                    BattleSuit = Convert.ToByte(vt(row, "battle_suit") ?? 0),
                };
                string s = vt(row, "assign_nodeid")?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(s))
                {
                    model.AssignNodeid = Guid.Empty;
                }
                else
                {
                    if (Guid.TryParse(s, out Guid guid))
                        model.AssignNodeid = guid;
                    else
                        model.AssignNodeid = Guid.Empty;
                }

                models.Add(model);
            }
            return models;
        }

        private void CopyTo(Ns source, Ns destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            source.AddressMask = destination.AddressMask;
            source.AssignNodeid = destination.AssignNodeid;
            source.CreationTime = destination.CreationTime;
            source.ServerNo = destination.ServerNo;
            source.ApplicationType = destination.ApplicationType;
            source.INetAddress = destination.INetAddress;
            source.PlatformName = destination.PlatformName;
            source.ServerAreaId = destination.ServerAreaId;
            source.BattleSuit = destination.BattleSuit;
        }

        private Error InternalSelect(Action<IDataAdapter, DbCommand> callback, out IList<Ns> models)
        {
            models = m_EmptyArray;

            Error error = InternalSelect(callback, out DataTable dataTable);
            if (error != Error.Error_Success)
            {
                return error;
            }
            try
            {
                using (dataTable)
                {
                    try
                    {
                        models = ConvertToAllModel(dataTable);
                    }
                    catch (Exception)
                    {
                        return Error.Error_UnableToConvertDateTableOrDataSetToAllTTypeModelList;
                    }
                }
            }
            catch (Exception)
            {

            }
            return Error.Error_Success;
        }

        private Error InternalFindOrDefault(Guid assignNodeid, out Ns model)
        {
            model = null;
            Error error = InternalSelect((adapter, command) =>
            {
                command.CommandText = "SELECT * FROM ns_info WHERE assign_nodeid=@assign_nodeid LIMIT 1";
                command.Parameters.Add(CreateParameter(adapter, "@assign_nodeid", assignNodeid.ToString()));
            }, out IList<Ns> models);
            if (error != Error.Error_Success)
            {
                return error;
            }
            if (models != null && models.Count > 0)
            {
                model = models[0];
            }
            return Error.Error_Success;
        }

        private Error AddToDatabaseTable(Ns model)
        {
            if (model == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }
            Error error = InternalExecuteNonQuery((adapter, command) =>
            {
                command.CommandText = @"INSERT INTO ns_info(application_type, address_mask, platform_name, server_no, assign_nodeid, inet_address, creation_time, serverarea_id, battle_suit)
                            VALUE(@application_type, @address_mask, @platform_name, @server_no, @assign_nodeid, @inet_address, @creation_time, @serverarea_id, @battle_suit)";
                command.Parameters.Add(CreateParameter(adapter, "@application_type", (int)model.ApplicationType));
                command.Parameters.Add(CreateParameter(adapter, "@address_mask", model.AddressMask ?? string.Empty));
                command.Parameters.Add(CreateParameter(adapter, "@platform_name", model.PlatformName ?? string.Empty));
                command.Parameters.Add(CreateParameter(adapter, "@server_no", model.ServerNo));
                command.Parameters.Add(CreateParameter(adapter, "@assign_nodeid", model.AssignNodeid.ToString()));
                command.Parameters.Add(CreateParameter(adapter, "@inet_address", model.INetAddress ?? string.Empty));
                command.Parameters.Add(CreateParameter(adapter, "@creation_time", model.CreationTime));
                command.Parameters.Add(CreateParameter(adapter, "@serverarea_id", model.ServerAreaId));
                command.Parameters.Add(CreateParameter(adapter, "@battle_suit", model.BattleSuit));
            }, out int nonquerying);
            if (error != Error.Error_Success)
            {
                return error;
            }
            if (nonquerying <= 0)
            {
                return Error.Error_AddToDatabaseAfterNonQueryTheValueIsLessOrEqualsThanZero;
            }
            return Error.Error_Success;
        }

        private const int MAX_TRY_NEW_NODEID_NUM = 5;

        // 备案应用服务器
        public virtual Error PutOnRecord(Ns ns)
        {
            if (ns == null)
            {
                return Error.Error_NsInstanceModelIsNullReferences;
            }
            Error error = InternalSelect((adapter, command) =>
            {
                if (ns.ApplicationType == ApplicationType.ApplicationType_GameServer || ns.ApplicationType == ApplicationType.ApplicationType_CrossServer)
                {
                    command.CommandText = "SELECT * FROM ns_info WHERE platform_name=@platform_name AND server_no=@server_no";
                    command.Parameters.Add(CreateParameter(adapter, "@platform_name", ns.PlatformName));
                    command.Parameters.Add(CreateParameter(adapter, "@server_no", ns.ServerNo));
                }
                else
                {
                    command.CommandText = "SELECT * FROM ns_info WHERE application_type=@application_type AND address_mask=@address_mask";
                    command.Parameters.Add(CreateParameter(adapter, "@address_mask", ns.AddressMask ?? string.Empty));
                    command.Parameters.Add(CreateParameter(adapter, "@application_type", (int)ns.ApplicationType));
                }
            }, out IList<Ns> models);
            if (error != Error.Error_Success)
            {
                return error;
            }
            if (models == null || models.Count <= 0)
            {
                Guid assignNodeid = Guid.Empty;
                for (int i = 0; i < MAX_TRY_NEW_NODEID_NUM; i++)
                {
                    assignNodeid = Guid.NewGuid();
                    error = InternalFindOrDefault(assignNodeid, out Ns model);
                    if (error != Error.Error_Success)
                    {
                        return error;
                    }

                    if (model != null)
                    {
                        assignNodeid = Guid.Empty;
                        continue;
                    }

                    ns.CreationTime = DateTime.Now;
                    ns.AssignNodeid = assignNodeid;
                    break;
                }
                if (assignNodeid == Guid.Empty)
                {
                    return Error.Error_AttemptsToGenerateMoreThanOneNodeidWereInvalid;
                }
                error = AddToDatabaseTable(ns);
                if (error != Error.Error_Success)
                {
                    return error;
                }
            }
            else
            {
                CopyTo(ns, models[0]);
            }
            return Error.Error_Success;
        }

        private static string GetNsCertKey(Guid id)
        {
            return "ns.cert.info." + id.ToString(); 
        }

        private static string GetNsCertLockKey(Guid id)
        {
            return "ns.cert.lock." + id.ToString();
        }

        public virtual Ns FindOrDefault(Guid id, out Error error)
        {
            Ns cert = null;
            error = CacheAccessor.GetClient((redis) =>
            {
                Error errno = CacheAccessor.AcquireLock(redis, GetNsCertLockKey(id), out IDisposable locker);
                if (errno != Error.Error_Success)
                    return errno;
                try
                {
                    string certkey = GetNsCertKey(id);
                    errno = CacheAccessor.GetValue(redis, certkey, out cert);
                    if (cert != null)
                        return Error.Error_Success;
                    errno = InternalFindOrDefault(id, out cert);
                    if (cert != null)
                        errno = CacheAccessor.SetValue(redis, certkey, cert);
                    return errno;
                }
                finally
                {
                    CacheAccessor.Unlock(locker);
                }
            });
            return cert;
        }
    }
}