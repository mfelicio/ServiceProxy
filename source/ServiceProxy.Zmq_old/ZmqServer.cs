using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq
{
    public class ZmqServer : IDisposable
    {
        private readonly string inboundAddress;
        private readonly string outboundAddress;

        private readonly ZMQ.Context zmqContext;
        private readonly Guid identity;

        private long running;
        private volatile Task receiveRequestsTask;
        private volatile Task sendResponsesTask;

        private readonly BlockingCollection<Tuple<byte[], byte[]>> responsesQueue;

        private readonly IServiceFactory serviceFactory;

        public ZmqServer(ZMQ.Context zmqContext,
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
            using (var inboundSocket = this.zmqContext.Socket(ZMQ.SocketType.DEALER))
            {
                //Same identity as outbound socket
                inboundSocket.Identity = this.identity.ToByteArray();

                //Connect to inbound address
                inboundSocket.Connect(this.inboundAddress);

                var timeout = 100;

                byte[] callerId;
                byte[] requestBytes;

                while (Interlocked.Read(ref this.running) == 1)
                {
                    callerId = inboundSocket.Recv(timeout);
                    if (callerId != null)
                    {
                        requestBytes = inboundSocket.Recv();
                        this.OnRequest(callerId, requestBytes);
                    }
                }
            }
        }

        private void SendResponses()
        {
            using (var outboundSocket = this.zmqContext.Socket(ZMQ.SocketType.DEALER))
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
                        outboundSocket.Send(response.Item2, ZMQ.SendRecvOpt.NOBLOCK); //response
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
