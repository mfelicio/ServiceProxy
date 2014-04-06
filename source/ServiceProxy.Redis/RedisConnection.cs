using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    public class RedisConnection
    {
        private readonly ConnectionMultiplexer connectionManager;

        public RedisConnection(string host, int port = 6379, string password = null)
        {
            var options = new ConfigurationOptions();
            options.Password = password;
            options.EndPoints.Add(host, port);
            options.AbortOnConnectFail = false;

            this.connectionManager = ConnectionMultiplexer.Connect(options);
        }

        public RedisConnection(ConnectionMultiplexer connectionMultiplexer)
        {
            this.connectionManager = connectionMultiplexer;
        }

        public IDatabase GetClient()
        {
            return this.connectionManager.GetDatabase();
        }

        public ISubscriber GetSubscriber()
        {
            return this.connectionManager.GetSubscriber();
        }

    }
}
