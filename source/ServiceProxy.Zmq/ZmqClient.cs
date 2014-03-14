using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ServiceProxy.Zmq
{
    public class ZmqClient : IClient, IDisposable
    {
        private readonly string inboundAddress;
        private readonly string outboundAddress;

        private readonly ZeroMQ.ZmqContext zmqContext;
        private readonly Guid identity;

        private long running;
        private volatile Task sendRequestsTask;
        private volatile Task receiveResponsesTask;

        private long nextId;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ResponseData>> requestCallbacks;

        private readonly BlockingCollection<byte[]> requestsQueue;

        public ZmqClient(ZeroMQ.ZmqContext zmqContext,
                         string inboundAddress,
                         string outboundAddress)
        {
            this.zmqContext = zmqContext;
            
            this.inboundAddress = inboundAddress;
            this.outboundAddress = outboundAddress;

            this.identity = zmqContext.NewIdentity();

            this.nextId = 0;
            this.requestCallbacks = new ConcurrentDictionary<string, TaskCompletionSource<ResponseData>>();

            this.requestsQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>(), int.MaxValue);
        }

        public Task<ResponseData> Request(RequestData request)
        {
            this.EnsureIsRunning();

            var requestId = this.NextId();

            var callback = new TaskCompletionSource<ResponseData>();
            this.requestCallbacks[requestId] = callback;

            Task.Run(() =>
            {
                var zmqRequest = new ZmqRequest(requestId, request);
                var zmqRequestBytes = zmqRequest.ToBinary();

                this.requestsQueue.TryAdd(zmqRequestBytes);
            });

            return callback.Task;
        }

        private string NextId()
        {
            return Interlocked.Increment(ref this.nextId).ToString();
        }

        private void EnsureIsRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 0)
            {
                this.sendRequestsTask = Task.Factory.StartNew(this.SendRequests, TaskCreationOptions.LongRunning);
                this.receiveResponsesTask = Task.Factory.StartNew(this.ReceiveResponses, TaskCreationOptions.LongRunning);
            }
        }

        private void EnsureIsNotRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 0, 1) == 1)
            {
                Task.WaitAll(this.sendRequestsTask, this.receiveResponsesTask);
                this.sendRequestsTask = null;
                this.receiveResponsesTask = null;
            }
        }

        private void SendRequests()
        {
            using (var outboundSocket = this.zmqContext.CreateWriteonlySocket(ZeroMQ.SocketType.DEALER))
            {
                //Same identity as inbound socket
                outboundSocket.Identity = this.identity.ToByteArray();

                //Connect to outbound address
                outboundSocket.Connect(this.outboundAddress);

                byte[] request;
                while (Interlocked.Read(ref this.running) == 1)
                {
                    while (this.requestsQueue.TryTake(out request, 100))
                    {
                        outboundSocket.Send(request);
                    }
                }
            }
        }

        private void ReceiveResponses()
        {
            using (var inboundSocket = this.zmqContext.CreateNonBlockingReadonlySocket(ZeroMQ.SocketType.DEALER, TimeSpan.FromMilliseconds(100)))
            {
                //Same identity as outbound socket
                inboundSocket.Identity = this.identity.ToByteArray();

                //Connect to inbound address
                inboundSocket.Connect(this.inboundAddress);

                byte[] buffer = new byte[1024];
                int readBytes;

                byte[] response;
                while (Interlocked.Read(ref this.running) == 1)
                {
                    response = inboundSocket.Receive(buffer, out readBytes);
                    if (readBytes > 0)
                    {
                        this.OnResponse(response.Slice(readBytes));
                    }
                }
            }
        }

        private void OnResponse(byte[] response)
        {
            Task.Run(() =>
            {
                var zmqResponse = ZmqResponse.FromBinary(response);

                TaskCompletionSource<ResponseData> callback;
                if (this.requestCallbacks.TryRemove(zmqResponse.RequestId, out callback))
                {
                    callback.SetResult(zmqResponse.Response);
                }
            });
        }

        public void Dispose()
        {
            this.EnsureIsNotRunning();
        }
    }
}
