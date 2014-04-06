using ServiceProxy;
using StackExchange.Redis;
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
        private readonly RedisConnection connection;
        private readonly IServiceFactory serviceFactory;

        private readonly string serviceQueue;

        private long receiveState;
        private volatile Task receiveTask;

        public RedisServer(RedisConnection connection, string serviceQueue, IServiceFactory serviceFactory)
        {
            this.connection = connection;
            this.serviceFactory = serviceFactory;
            this.serviceQueue = serviceQueue;
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
            int delayTimeout = 1;
            byte[] rawRequest;

            var redis = this.connection.GetClient();

            while (Interlocked.Read(ref this.receiveState) == 1)
            {
                rawRequest = await redis.ListRightPopAsync(this.serviceQueue).IgnoreException(typeof(RedisException));
                if (rawRequest == null)
                {
                    await Task.Delay(delayTimeout);
                    //increase timeout until delayMaxTimeout
                    continue;
                }

                var redisRequestBytes = rawRequest;

                Task.Run(() =>
                {
                    var redisRequest = RedisRequest.FromBinary(redisRequestBytes);
                    this.OnRequest(redis, redisRequest);
                });
            }
        }

        private void OnRequest(IDatabase redis, RedisRequest redisRequest)
        {
            var service = this.serviceFactory.CreateService(redisRequest.Request.Service);

            service.Process(redisRequest.Request)
                   .ContinueWith(t =>
                   {
                       var response = t.Result;

                       var redisResponse = new RedisResponse(redisRequest.Id, response);
                       var redisResponseBytes = redisResponse.ToBinary();

                       redis.ListLeftPushAsync(redisRequest.ReceiveQueue, redisResponseBytes);
                   });
        }

        public void Dispose()
        {
            this.EnsureIsNotReceiving();
        }
    }
}
