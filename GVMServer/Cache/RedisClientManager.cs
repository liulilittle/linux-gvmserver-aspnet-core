namespace GVMServer.Cache
{
    using System;
    using Microsoft.Extensions.Configuration;
    using ServiceStack.Redis;

    public static class RedisClientManager
    {
        private static PooledRedisClientManager pool = null;
        private static readonly object syncobj = new object();

        public static PooledRedisClientManager Create(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            string[] readWriteHosts = configuration.GetSection("readWriteHosts").Get<string[]>();
            string[] readOnlyHosts = configuration.GetSection("readOnlyHosts").Get<string[]>();
            int maxWritePoolSize = configuration.GetSection("MaxWritePoolSize").Get<int>();
            int maxReadPoolSize = configuration.GetSection("MaxReadPoolSize").Get<int>();
            int poolSizeMultiplier = configuration.GetSection("PoolSizeMultiplier").Get<int>();
            int poolTimeOutSeconds = configuration.GetSection("PoolTimeOutSeconds").Get<int>();

            return Create(readWriteHosts, readOnlyHosts, maxWritePoolSize, maxReadPoolSize, poolSizeMultiplier, poolTimeOutSeconds);
        }

        public static PooledRedisClientManager Create(
            string[] readWriteHosts, 
            string[] readOnlyHosts,
            int maxWritePoolSize,
            int maxReadPoolSize,
            int poolSizeMultiplier,
            int poolTimeOutSeconds,
            int initialDB = 0)
        {
            if (maxWritePoolSize <= 0)
            {
                maxWritePoolSize = Environment.ProcessorCount;
            }
            if (maxReadPoolSize <= 0)
            {
                maxReadPoolSize = Environment.ProcessorCount;
            }
            if (poolSizeMultiplier <= 0)
            {
                poolSizeMultiplier = Environment.ProcessorCount << 2;
            }
            if (poolTimeOutSeconds <= 0)
            {
                poolTimeOutSeconds = 5 << 2;
            }
            lock (syncobj)
            {
                if (pool != null)
                {
                    pool.Dispose();
                    pool = null;
                }
                pool = new PooledRedisClientManager(
                    readWriteHosts,
                    readOnlyHosts,
                    new RedisClientManagerConfig()
                    {
                        AutoStart = true,
                        MaxWritePoolSize = maxWritePoolSize,
                        MaxReadPoolSize = maxReadPoolSize,
                    }, initialDB, poolSizeMultiplier, poolTimeOutSeconds);
            }
            return pool;
        }

        public static PooledRedisClientManager GetDefault()
        {
            lock (syncobj)
            {
                return pool;
            }
        }
    }
}
