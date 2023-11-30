namespace GVMServer.Web.Database.Basic
{
    using System.Data.Common;
    using GVMServer.Web.Database.MySql;

    public class DataNode
    {
        public MySqlAdapter Master { get; set; }

        public MySqlAdapter Salve { get; set; }
    }

    public interface IDataAdapter
    {
        bool KeepAlived { get; }

        bool Available { get; }

        DbConnection GetConnection();

        DbCommand CreateCommand();

        DbDataAdapter CreateAdapter();

        DbParameter CreateParameter();
    }
}
