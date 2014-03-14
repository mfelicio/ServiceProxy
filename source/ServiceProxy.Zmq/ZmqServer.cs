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
    public class ZmqServer : IDisposable
    {
        private readonly string inboundAddress;
        private readonly string outboundAddress;

        private readonly ZeroMQ.ZmqContext zmqContext;
        private readonly Guid identity;

        private long running;
        private volatile Task receiveRequestsTask;
        private volatile Task sendResponsesTask;

        private readonly BlockingCollection<Tuple<byte[], byte[]>> responsesQueue;

        private readonly IServiceFactory serviceFactory;

        public ZmqServer(ZeroMQ.ZmqContext zmqContext,
                         string inboundAddress,
                         string outboundAddress,
                         IServiceFactory serviceFactory)
        {
            this.zmqContext = zmqContext;

            this.inboundAddress = inboundAddress;
            this.outboundAddress = outboundAddress;

            this.identity = zmqContext.NewIdentity();

            this.responsesQueue = new BlockingCollection<Tuple<byte[], byte[]>>(new ConcurrentQueue<Tuple<byte[], byte[]>>(), int.MaxValue);

            this.serviceFactory = serviceFactory;
        }

        public void Listen()
        {
            this.EnsureIsRunning();
        }

        private void EnsureIsRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 0)
            {
                this.receiveRequestsTask = Task.Factory.StartNew(this.ReceiveRequests, TaskCreationOptions.LongRunning);
                this.sendResponsesTask = Task.Factory.StartNew(this.SendResponses, TaskCreationOptions.LongRunning);
            }
        }

        private void EnsureIsNotRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 0, 1) == 1)
            {
                Task.WaitAll(this.receiveRequestsTask, this.sendResponsesTask);
                this.receiveRequestsTask = null;
                this.sendResponsesTask = null;
            }
        }

        private void ReceiveRequests()
        {
            using (var inboundSocket = this.zmqContext.CreateNonBlockingReadonlySocket(ZeroMQ.SocketType.DEALER, TimeSpan.FromMilliseconds(100)))
            {
                //Same identity as outbound socket
                inboundSocket.Identity = this.identity.ToByteArray();

                //Connect to inbound address
                inboundSocket.Connect(this.inboundAddress);

                byte[] callerId;
                byte[] requestBytes;

                byte[] buffer = new byte[1024];
                int readBytes;

                while (Interlocked.Read(ref this.running) == 1)
                {
                    callerId = inboundSocket.Receive(buffer, out readBytes); 
                    if (readBytes > 0)
                    {
                        callerId = callerId.Slice(readBytes);
                        requestBytes = inboundSocket.Receive(buffer, out readBytes);
                        this.OnRequest(callerId, requestBytes.Slice(readBytes));
                    }
                }
            }
        }

        private void SendResponses()
        {
            using (var outboundSocket = this.zmqContext.CreateWriteonlySocket(ZeroMQ.SocketType.DEALER))
            {
                //Same identity as inbound socket
                outboundSocket.Identity = this.identity.ToByteArray();

                //Connect to outbound address
                outboundSocket.Connect(this.outboundAddress);

                Tuple<byte[], byte[]> response;
                while (Interlocked.Read(ref this.running) == 1)
                {
                    while (this.responsesQueue.TryTake(out response, 100))
                    {
                        outboundSocket.SendMore(response.Item1); //callerId
                        outboundSocket.Send(response.Item2); //response
                    }
                }
            }
        }

        private void OnRequest(byte[] callerId, byte[] requestBytes)
        {
            Task.Run(() =>
            {
                var zmqRequest = ZmqRequest.FromBinary(requestBytes);

                var service = this.serviceFactory.CreateService(zmqRequest.Request.Service);

                service.Process(zmqRequest.Request)
                       .ContinueWith(t =>
                       {
                           var response = t.Result;

                           var zmqResponse = new ZmqResponse(zmqRequest.Id, response);
                           var zmqResponseBytes = zmqResponse.ToBinary();

                           this.responsesQueue.TryAdd(new Tuple<byte[], byte[]>(callerId, zmqResponseBytes));
                       });
            });
        }

        public void Dispose()
        {
            this.EnsureIsNotRunning();
        }
    }
}
