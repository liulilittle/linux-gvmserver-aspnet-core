namespace GVMServer.Web.Database
{
    using System;
    using System.Collections.Concurrent;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Threading;
    using GVMServer.DDD.Service;
    using IDataAdapter = GVMServer.Web.Database.Basic.IDataAdapter;

    public class DatabaseConnectionPool : IServiceBase, IDisposable
    {
        private ConcurrentDictionary<IDataAdapter, IDataAdapter> m_Adapters = null;
        private Thread m_Maintenance = null;
        private volatile bool m_disposed = false;
        private readonly object m_syncobj = new object();

        private const int HEARTBEAT_INTERVAL_TIME = 30000;

        ~DatabaseConnectionPool()
        {
            this.Dispose();
        }

        private ConcurrentDictionary<IDataAdapter, IDataAdapter> LazyInitValue()
        {
            lock (this.m_syncobj)
            {
                if (this.m_disposed)
                {
                    return null;
                }

                if (this.m_Adapters == null)
                {
                    this.m_Adapters = new ConcurrentDictionary<IDataAdapter, IDataAdapter>();
                }

                if (this.m_Maintenance == null)
                {
                    this.m_Maintenance = new Thread(() =>
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        while (!this.m_disposed)
                        {
                            foreach (IDataAdapter adapter in m_Adapters.Values)
                            {
                                DbConnection connection = adapter.GetConnection();
                                if (connection == null)
                                {
                                    continue;
                                }

                                if (connection.State != ConnectionState.Open)
                                {
                                    continue;
                                }

                                if (!stopwatch.IsRunning)
                                {
                                    stopwatch.Start();
                                }

                                if (stopwatch.ElapsedMilliseconds >= HEARTBEAT_INTERVAL_TIME)
                                {
                                    Heartbeat(connection);
                                }
                            }

                            if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds >= HEARTBEAT_INTERVAL_TIME)
                            {
                                stopwatch.Reset();
                            }

                            Thread.Sleep(500);
                        }
                    });

                    this.m_Maintenance.IsBackground = true;
                    this.m_Maintenance.Priority = ThreadPriority.Lowest;
                    this.m_Maintenance.Start();
                }
            }
            return this.m_Adapters;
        }

        private bool Heartbeat(DbConnection connection)
        {
            try
            {
                if (connection == null || connection.State != ConnectionState.Open)
                {
                    return true;
                }

                lock (connection)
                {
                    DbCommand command = null;
                    try
                    {
                        using (command = connection.CreateCommand())
                        {
                            try
                            {
                                command.CommandText = "SELECT 1 = 1";
                                using (DbDataReader dataReader = command.ExecuteReader())
                                {
                                    do
                                    {
                                        while (dataReader.Read()) ;
                                    } while (dataReader.NextResult());
                                    dataReader.Close();
                                }

                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (command != null)
                        {
                            try
                            {
                                command.Dispose();
                            }
                            catch (Exception) { }
                        }
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual bool Register(IDataAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }
            var d = this.LazyInitValue();
            if (d == null)
            {
                throw new InvalidOperationException();
            }
            return d.TryAdd(adapter, adapter);
        }

        public virtual bool Unregister(IDataAdapter adapter)
        {
            if (adapter == null)
            {
                return false;
            }
            var d = this.LazyInitValue();
            if (d == null)
            {
                throw new InvalidOperationException();
            }
            return d.TryRemove(adapter, out adapter);
        }

        public void Dispose()
        {
            lock (this.m_syncobj)
            {
                if (!this.m_disposed)
                {
                    this.m_disposed = true;
                    if (this.m_Adapters != null)
                    {
                        this.m_Adapters.Clear();
                    }
                    this.m_Adapters = null;
                    this.m_Maintenance = null;
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
