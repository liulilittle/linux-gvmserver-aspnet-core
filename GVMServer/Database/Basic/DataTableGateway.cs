namespace GVMServer.Web.Database.Basic
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using GVMServer.AOP.Data;
    using GVMServer.DDD.Service;

    public class DataTableGateway : IServiceBase
    {
        private readonly IDataAdapter m_adapter = null;

        public DataTableGateway(IDataAdapter adapter)
        {
            this.m_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public IDataAdapter GetAdapter()
        {
            return this.m_adapter;
        }

        public DataSet Select(DbCommand command)
        {
            using (DbDataAdapter adapter = this.m_adapter.CreateAdapter())
            {
                DbConnection connection = command.Connection;
                if (connection == null)
                {
                    try
                    {
                        connection = this.m_adapter.GetConnection();
                        if (connection != null)
                        {
                            if (connection.State != ConnectionState.Open)
                            {
                                connection.Open();
                            }

                            command.Connection = connection;
                        }
                    }
                    catch (Exception)
                    {
                        if (connection != null)
                        {
                            connection.Dispose();
                        }

                        return null;
                    }
                }

                if (connection == null)
                {
                    return null;
                }

                adapter.SelectCommand = command;
                lock (connection)
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        adapter.Fill(ds);
                    }
                    catch (Exception)
                    {
                        ds.Dispose();
                        ds = null;
                    }

                    TryCloseConnection(connection);
                    return ds;
                }
            }
        }

        public IList<T> Select<T>(DbCommand command)
        {
            using (DataSet ds = this.Select(command))
            {
                if (ds == null)
                {
                    return null;
                }
                try
                {
                    if (typeof(T) == typeof(object))
                    {
                        return (IList<T>)DataModelProxyConverter.GetInstance().ToList(ds.Tables[0]);
                    }
                    else
                    {
                        return (IList<T>)DataModelProxyConverter.GetInstance().ToList<T>(ds.Tables[0]);
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public int ExecuteNonQuery(DbCommand command)
        {
            DbConnection connection = command.Connection;
            if (connection == null)
            {
                try
                {
                    connection = this.m_adapter.GetConnection();
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    command.Connection = connection;
                }
                catch (Exception)
                {
                    if (connection != null)
                    {
                        connection.Dispose();
                    }

                    return -1;
                }
            }

            if (connection == null)
            {
                return -1;
            }

            lock (connection)
            {
                int nonquery = 0;
                try
                {
                    nonquery = command.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    nonquery = -1;
                }

                TryCloseConnection(connection);
                return nonquery;
            }
        }

        private bool TryCloseConnection(DbConnection connection)
        {
            IDataAdapter adapter = this.m_adapter;
            try
            {
                if (adapter == null || !adapter.KeepAlived)
                {
                    IDisposable disposable = connection as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                        return true;
                    }
                }

                return connection.State == ConnectionState.Closed;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
