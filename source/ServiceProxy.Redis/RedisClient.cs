using ServiceProxy;
using StackExchange.Redis;
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
        private readonly RedisConnection connection;

        private readonly string receiveQueue;
        private readonly string sendQueue;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ResponseData>> requestCallbacks;

        private long nextId;
        private long receiveState;
        private volatile Task receiveTask;

        public RedisClient(RedisConnection connection, string receiveQueue, string sendQueue)
        {
            this.connection = connection;
            this.receiveQueue = receiveQueue;
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

            var redisRequest = new RedisRequest(this.receiveQueue, requestId, request);
            var redisRequestBytes = redisRequest.ToBinary();

            var redis = this.connection.GetClient();
            var lpushTask = redis.ListLeftPushAsync(sendQueue, redisRequestBytes);

            var callback = new TaskCompletionSource<ResponseData>();
            this.requestCallbacks[requestId] = callback;

            if (token != CancellationToken.None)
            {
                token.Register(() =>
                {
                    this.OnRequestCancelled(requestId);
                });
            }

            lpushTask.ContinueWith(t =>
            {
                if (lpushTask.Exception != null)
                {
                    this.OnRequestError(requestId, lpushTask.Exception.InnerException);
                }
            });

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
            int delayTimeout = 1;
            byte[] rawResponse;

            var redis = this.connection.GetClient();

            while (Interlocked.Read(ref this.receiveState) == 1)
            {
                rawResponse = await redis.ListRightPopAsync(receiveQueue).IgnoreException(typeof(RedisException));
                if (rawResponse == null)
                {
                    await Task.Delay(delayTimeout);
                    //maybe increase delayTimeout a little bit?
                    continue;
                }

                var redisResponseBytes = rawResponse;

                Task.Run(() =>
                {
                    var redisResponse = RedisResponse.FromBinary(redisResponseBytes);
                    this.OnResponse(redisResponse);
                });
            }

        }

        private void OnResponse(RedisResponse redisResponse)
        {
            TaskCompletionSource<ResponseData> callback;
            if (this.requestCallbacks.TryRemove(redisResponse.RequestId, out callback))
            {
                callback.SetResult(redisResponse.Response);
            }
        }

        private void OnRequestError(string requestId, Exception exception)
        {
            TaskCompletionSource<ResponseData> callback;
            if (this.requestCallbacks.TryRemove(requestId, out callback))
            {
                callback.TrySetException(exception);
            }
        }

        private void OnRequestCancelled(string requestId)
        {
            TaskCompletionSource<ResponseData> _;
            this.requestCallbacks.TryRemove(requestId, out _);
        }

        public void Dispose()
        {
            this.EnsureIsNotReceiving();
            this.requestCallbacks.Clear();
        }
    }
}
