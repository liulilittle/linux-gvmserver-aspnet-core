namespace GVMServer.Web.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using GVMServer.Cache;
    using GVMServer.Linq;
    using GVMServer.Web.Database.Basic;
    using GVMServer.Web.Model;
    using Microsoft.Extensions.Configuration;
    using ServiceStack.Redis;

    public class AccountGateway : IAccountGateway
    {
        private DataNode[] m_DateNodes = null;

        public readonly int PartitionTableSizeOf = 0;
        public readonly int PartitionTableCount = 0;
        public readonly bool Usable = false;

        private const string ACCOUNT_LOCK_KEY = "database.account.lock.maxaccountid";
        private const string ACCOUNT_MAXACCOUNTID_KEY = "database.account.atomic.maxaccountid";

        public AccountGateway(DatabaseConnectionPool pool)
        {
            this.m_DateNodes = Startup.GetDefaultConfiguration().
                 GetSection("Database").
                 GetSection("MySql").
                 GetSection("DataNodes").Get<DataNode[]>();

            this.PartitionTableSizeOf = Startup.GetDefaultConfiguration().
                GetSection("Database").
                GetSection("MySql").
                GetSection("PartitionTableSizeOf").Get<int>();

            this.PartitionTableCount = Startup.GetDefaultConfiguration().
                GetSection("Database").
                GetSection("MySql").
                GetSection("PartitionTableCount").Get<int>();

            this.Usable = Startup.GetDefaultConfiguration().
                GetSection("Database").
                GetSection("MySql").
                GetSection("Usable").Get<bool>();

            if (this.Usable)
            {
                Task load = new Task(() => this.GetMaxAccountId());
                load.Start();
            }
        }

        private DataNode GetNode(int supplement, out int maxaccountid)
        {
            DataNode node = null;
            using (IRedisClient redis = RedisClientManager.GetDefault().GetClient())
            {
                maxaccountid = redis.Get<int>(ACCOUNT_MAXACCOUNTID_KEY);
                if (maxaccountid < 0)
                {
                    node = this.m_DateNodes[0];
                }
                else
                {
                    int datanode = (maxaccountid + supplement) /
                        (this.PartitionTableCount * this.PartitionTableSizeOf); // 数据节点
                    if (datanode > this.m_DateNodes.Length)
                    {
                        throw new SystemException("配置的数据存储节点不足");
                    }
                    node = this.m_DateNodes[datanode];
                }
            }
            return node;
        }

        private DataTableGateway GetGateway(int supplement, out int maxaccountid)
        {
            DataNode node = this.GetNode(supplement, out maxaccountid);
            if (node.Master.Available)
            {
                return new DataTableGateway(node.Master);
            }
            else if (node.Salve.Available)
            {
                return new DataTableGateway(node.Salve);
            }
            throw new SystemException("此数据存储节点已崩溃");
        }

        private class AccountPartitionTable
        {
            public string PartitionTableName { get; set; }

            public int MaxAccountId { get; set; }
        }

        public int GetMaxAccountId()
        {
            if (!this.Usable)
            {
                throw new InvalidOperationException("未配置使用数据库功能");
            }
            using (IRedisClient redis = RedisClientManager.GetDefault().GetClient())
            {
                if (redis.ContainsKey(ACCOUNT_MAXACCOUNTID_KEY))
                {
                    return redis.Get<int>(ACCOUNT_MAXACCOUNTID_KEY);
                }
                int maxaccountid = 0;
                using (IDisposable locker = redis.AcquireLock(ACCOUNT_LOCK_KEY, AccountInfo.GetAcquireLockTimeout()))
                {
                    foreach (var node in this.m_DateNodes)
                    {
                        DataTableGateway gateway = new DataTableGateway(node.Master);

                        int result = this.GetMaxAccountId(gateway);
                        if (result > maxaccountid)
                        {
                            maxaccountid = result;
                        }
                    }
                    redis.Set(ACCOUNT_MAXACCOUNTID_KEY, maxaccountid);
                }
                return maxaccountid;
            }
        }

        private int GetMaxAccountId(DataTableGateway gateway)
        {
            using (var command = gateway.GetAdapter().CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT table_name AS PartitionTableName FROM information_schema.tables WHERE SUBSTR(table_name, 1, LENGTH('account_data_table_')) = 'account_data_table_';";

                IList<AccountPartitionTable> tables = gateway.Select<AccountPartitionTable>(command);
                if (tables.IsNullOrEmpty())
                {
                    throw new SystemException("未配置任何有效的账户分表");
                }
                else
                {
                    string sql = string.Empty;
                    do
                    {
                        foreach (AccountPartitionTable table in tables)
                        {
                            if (!string.IsNullOrEmpty(sql))
                            {
                                sql += "UNION ALL\r\n";
                            }
                            sql += $"SELECT COUNT(1) AS c FROM {table.PartitionTableName}\r\n";
                        }
                        sql = $"SELECT MAX(c) AS MaxAccountId FROM ({sql}) AS dataTableChunkCount;";
                        command.CommandText = sql;
                    } while (false);
                    tables = gateway.Select<AccountPartitionTable>(command);
                    if (tables.IsNullOrEmpty())
                    {
                        return 0;
                    }
                    else
                    {
                        return tables[0].MaxAccountId;
                    }
                }
            }
        }

        public int GetAccountId(AccountInfo account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var gateway = this.GetGateway(0, out int maxaccountid);
            using (var command = gateway.GetAdapter().CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "proc_CreateAccount";

                var parameter = gateway.GetAdapter().CreateParameter();
                parameter.Value = account.UserId;
                parameter.ParameterName = "p_userid";
                parameter.Direction = ParameterDirection.Input;
                parameter.DbType = DbType.String;
                command.Parameters.Add(parameter);

                parameter = gateway.GetAdapter().CreateParameter();
                parameter.Value = account.AppId;
                parameter.ParameterName = "p_appid";
                parameter.Direction = ParameterDirection.Input;
                parameter.DbType = DbType.String;
                command.Parameters.Add(parameter);

                parameter = gateway.GetAdapter().CreateParameter();
                parameter.Value = account.ChannelId;
                parameter.ParameterName = "p_channelid";
                parameter.Direction = ParameterDirection.Input;
                parameter.DbType = DbType.String;
                command.Parameters.Add(parameter);

                parameter = gateway.GetAdapter().CreateParameter();
                parameter.Value = account.ChannelUserId;
                parameter.ParameterName = "p_channeluid";
                parameter.Direction = ParameterDirection.Input;
                parameter.DbType = DbType.String;
                command.Parameters.Add(parameter);

                var accountIdParameter = gateway.GetAdapter().CreateParameter();
                accountIdParameter.Value = maxaccountid;
                accountIdParameter.ParameterName = "p_outaid";
                accountIdParameter.Direction = ParameterDirection.InputOutput;
                accountIdParameter.DbType = DbType.Int32;
                command.Parameters.Add(accountIdParameter);

                var isInsertedParamter = gateway.GetAdapter().CreateParameter();
                isInsertedParamter.Value = 0;
                isInsertedParamter.ParameterName = "p_inserted";
                isInsertedParamter.Direction = ParameterDirection.Output;
                isInsertedParamter.DbType = DbType.Int32;
                command.Parameters.Add(isInsertedParamter);

                int nonquery = gateway.ExecuteNonQuery(command);
                if (nonquery < 0)
                {
                    return 0;
                }

                int currentaccountid = 0;

                if (parameter.Value != DBNull.Value)
                {
                    currentaccountid = Convert.ToInt32(accountIdParameter.Value);
                }

                if (object.Equals(isInsertedParamter.Value, 1))
                {
                    using (IRedisClient redis = RedisClientManager.GetDefault().GetClient())
                    {
                        using (IDisposable locker = redis.AcquireLock(ACCOUNT_LOCK_KEY, AccountInfo.GetAcquireLockTimeout()))
                        {
                            if (currentaccountid > (redis.Get<int>(ACCOUNT_MAXACCOUNTID_KEY) + 1))
                            {
                                redis.Set(ACCOUNT_MAXACCOUNTID_KEY, currentaccountid);
                            }
                            else
                            {
                                redis.IncrementValue(ACCOUNT_MAXACCOUNTID_KEY);
                            }
                        }
                    }
                }

                return currentaccountid;
            }
        }
    }
}
