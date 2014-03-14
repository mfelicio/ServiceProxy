using BookSleeve;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    public class RedisDuplexConnection
    {
        private readonly RedisConnection sender;
        private readonly RedisConnection receiver;

        public RedisDuplexConnection(string host, int port = 6379, string password = null)
        {
            this.sender = new RedisConnection(host, port: port, password: password);
            this.receiver = new RedisConnection(host, port: port, password: password);

            this.Open();
        }

        public RedisConnection Sender { get { return this.sender; } }
        public RedisConnection Receiver { get { return this.receiver; } }

        public void Open()
        {
            this.sender.Open();
            this.receiver.Open();
        }

        public void Close()
        {
            this.sender.Dispose();
            this.receiver.Dispose();
        }
    }
}
