namespace GVMServer.Web.Database
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using MongoDB.Driver;

    public class MongoGateway : IMongoGateway
    {
        private IDictionary<string, IMongoDatabase> m_database = new ConcurrentDictionary<string, IMongoDatabase>();
        private IMongoClient m_mongo = null;
        private readonly object m_syncobj = new object();

        public MongoGateway()
        {
            string connections = Startup.GetDefaultConfiguration().
                GetSection("Database").
                GetSection("Mongo").
                GetValue<string>("connectionsString");
            m_mongo = new MongoClient(connections);
        }

        public virtual IMongoDatabase GetDatabase(string database)
        {
            if (string.IsNullOrEmpty(database))
            {
                return null;
            }

            lock (m_syncobj)
            {
                IMongoDatabase db = null;
                if (m_database.TryGetValue(database, out db))
                {
                    return db;
                }

                db = m_mongo.GetDatabase(database);
                if (db == null)
                {
                    return null;
                }

                m_database.Add(database, db);
                return db;
            }
        }
    }
}
