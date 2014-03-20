using ServiceProxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    public class RedisClient : IClient, IDisposable
    {
        private readonly RedisDuplexConnection connection;

        private readonly string[] receiveQueues;
        private readonly string sendQueue;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ResponseData>> requestCallbacks;

        private long nextId;
        private long receiveState;
        private volatile Task receiveTask;

        public RedisClient(RedisDuplexConnection connection, string receiveQueue, string sendQueue)
        {
            this.connection = connection;
            this.receiveQueues = new string[] { receiveQueue };
            this.sendQueue = sendQueue;
            this.nextId = 0;
            this.receiveState = 0;
            this.requestCallbacks = new ConcurrentDictionary<string, TaskCompletionSource<ResponseData>>();
        }

        private string NextId()
        {
            return Interlocked.Increment(ref this.nextId).ToString();
        }

        public Task<ResponseData> Request(RequestData request, CancellationToken token)
        {
            this.EnsureIsReceiving();

            var requestId = this.NextId();

            var redisRequest = new RedisRequest(this.receiveQueues[0], requestId, request);
            var redisRequestBytes = redisRequest.ToBinary();

            this.connection.Sender.Lists.AddFirst(0, this.sendQueue, redisRequestBytes);

            var callback = new TaskCompletionSource<ResponseData>();
            this.requestCallbacks[requestId] = callback;

            if (token != CancellationToken.None)
            {
                token.Register(() =>
                {
                    TaskCompletionSource<ResponseData> _;
                    this.requestCallbacks.TryRemove(requestId, out _);
                });
            }

            return callback.Task;
        }

        private void EnsureIsReceiving()
        {
            if (Interlocked.CompareExchange(ref this.receiveState, 1, 0) == 0)
            {
                this.receiveTask = Task.Factory.StartNew(() => this.Receive(), TaskCreationOptions.LongRunning).Unwrap();
            }
        }

        private void EnsureIsNotReceiving()
        {
            if (Interlocked.CompareExchange(ref this.receiveState, 0, 1) == 1)
            {
                this.receiveTask.Wait();
            }
        }

        private async Task Receive()
        {
            while (Interlocked.Read(ref this.receiveState) == 1)
            {
                var rawResponse = await this.connection.Receiver.Lists.BlockingRemoveLast(0, this.receiveQueues, 1);
                if (rawResponse == null)
                {
                    continue;
                }

                Task.Run(() =>
                {
                    var redisResponseBytes = rawResponse.Item2;
                    var redisResponse = RedisResponse.FromBinary(redisResponseBytes);

                    TaskCompletionSource<ResponseData> callback;
                    if (this.requestCallbacks.TryRemove(redisResponse.RequestId, out callback))
                    {
                        callback.SetResult(redisResponse.Response);
                    }

                });
            }

        }

        public void Dispose()
        {
            this.EnsureIsNotReceiving();
            this.requestCallbacks.Clear();
        }
    }
}
