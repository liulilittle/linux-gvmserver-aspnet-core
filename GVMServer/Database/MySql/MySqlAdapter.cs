namespace GVMServer.Web.Database.MySql
{
    using System;
    using System.Data;
    using System.Data.Common;
    using global::MySql.Data.MySqlClient;
    using GVMServer.DDD.Service;
    using IDataAdapter = GVMServer.Web.Database.Basic.IDataAdapter;

    public class MySqlAdapter : IDataAdapter, IDisposable
    {
        private MySqlConnection m_Connection = null;
        private bool m_Available = false;
        private bool m_disposed = false;
        private readonly object m_syncobj = new object();

        public bool KeepAlived => true;

        public bool Available => this.m_Available;

        public MySqlAdapter()
        {
            ServiceObjectContainer.Get<DatabaseConnectionPool>().Register(this);
        }

        ~MySqlAdapter()
        {
            this.Dispose();
        }

        public string Server { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        public int Port { get; set; }

        public override string ToString()
        {
            return $"Database={Database};Data Source={Server};User Id={UserName};Password={Password};pooling=false;CharSet=utf8;port={Port};";
        }

        private bool IsValidConfiguration()
        {
            if (string.IsNullOrEmpty(this.Server))
            {
                return false;
            }
            if (string.IsNullOrEmpty(this.UserName))
            {
                return false;
            }
            if (string.IsNullOrEmpty(this.Password))
            {
                return false;
            }
            if (string.IsNullOrEmpty(this.Database))
            {
                return false;
            }
            if (this.Port <= 0)
            {
                return false;
            }
            return true;
        }

        public DbConnection GetConnection()
        {
            InvalidOperationException exception = null;
            lock (this.m_syncobj)
            {
                if (this.m_disposed)
                {
                    exception = new InvalidOperationException();
                }
                else
                {
                    if (this.m_Connection == null)
                    {
                        if (!this.IsValidConfiguration())
                        {
                            return null;
                        }
                        this.m_Connection = new MySqlConnection(this.ToString());
                        this.m_Connection.Disposed += (sender, e) =>
                        {
                            lock (this.m_syncobj)
                            {
                                this.m_Available = false;
                                this.m_Connection = null;
                            }
                        };
                    }
                    try
                    {
                        if (this.m_Connection.State != ConnectionState.Open) // 可能只是连接池中取出来的
                        {
                            this.m_Connection.Open();
                            this.m_Available = true;
                        }
                    }
                    catch (Exception)
                    {
                        this.m_Available = false;
                        var connection = this.m_Connection;
                        this.m_Connection = null;
                        if (connection != null)
                        {
                            connection.Dispose();
                        }
                    }
                    return this.m_Connection;
                }
            }
            if (exception != null)
            {
                throw exception;
            }
            return null;
        }

        public DbCommand CreateCommand()
        {
            return new MySqlCommand();
        }

        public DbDataAdapter CreateAdapter()
        {
            return new MySqlDataAdapter();
        }

        public DbParameter CreateParameter()
        {
            return new MySqlParameter();
        }

        public void Dispose()
        {
            lock (this.m_syncobj)
            {
                if (!this.m_disposed)
                {
                    this.m_disposed = true;
                    var connection = this.m_Connection;
                    if (connection != null)
                    {
                        connection.Dispose();
                    }
                    this.m_Connection = null;
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
