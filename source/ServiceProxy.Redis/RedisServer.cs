using ServiceProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    public class RedisServer : IDisposable
    {
        private readonly RedisDuplexConnection connection;
        private readonly IServiceFactory serviceFactory;

        private readonly string[] serviceQueues;

        private long receiveState;
        private volatile Task receiveTask;

        public RedisServer(RedisDuplexConnection connection, string serviceQueue, IServiceFactory serviceFactory)
        {
            this.connection = connection;
            this.serviceFactory = serviceFactory;
            this.serviceQueues = new string[] { serviceQueue };
            this.receiveState = 0;
        }

        public void Listen()
        {
            this.EnsureIsReceiving();
        }

        private void EnsureIsReceiving()
        {
            if (Interlocked.CompareExchange(ref this.receiveState, 1, 0) == 0)
            {
                this.receiveTask = Task.Factory.StartNew(() => this.ReceiveRequests(), TaskCreationOptions.LongRunning).Unwrap();
            }
        }

        private void EnsureIsNotReceiving()
        {
            if (Interlocked.CompareExchange(ref this.receiveState, 0, 1) == 1)
            {
                this.receiveTask.Wait();
            }
        }

        private async Task ReceiveRequests()
        {
            while (Interlocked.Read(ref this.receiveState) == 1)
            {
                var rawRequest = await this.connection.Receiver.Lists.BlockingRemoveLast(0, this.serviceQueues, 1);
                if (rawRequest == null)
                {
                    continue;
                }

                Task.Run(() =>
                {
                    var redisRequestBytes = rawRequest.Item2;
                    var redisRequest = RedisRequest.FromBinary(redisRequestBytes);

                    var service = this.serviceFactory.CreateService(redisRequest.Request.Service);

                    service.Process(redisRequest.Request)
                           .ContinueWith(t =>
                           {
                               var response = t.Result;

                               var redisResponse = new RedisResponse(redisRequest.Id, response);
                               var redisResponseBytes = redisResponse.ToBinary();

                               this.connection.Sender.Lists.AddFirst(0, redisRequest.ReceiveQueue, redisResponseBytes);
                           });

                });
            }
        }

        public void Dispose()
        {
            this.EnsureIsNotReceiving();
        }
    }
}
