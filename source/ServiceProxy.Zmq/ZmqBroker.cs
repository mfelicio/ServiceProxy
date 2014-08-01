using Castle.Zmq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq
{
    public class ZmqBroker : IDisposable
    {
        private readonly string clientInboundAddress;
        private readonly string clientOutboundAddress;
        private readonly string serverInboundAddress;
        private readonly string serverOutboundAddress;

        private readonly IZmqContext zmqContext;

        private long running;
        private volatile Task forwardRequestsTask;
        private volatile Task forwardResponsesTask;

        public ZmqBroker(IZmqContext zmqContext,
                         string clientInboundAddress,
                         string clientOutboundAddress,
                         string serverInboundAddress,
                         string serverOutboundAddress)
        {
            this.zmqContext = zmqContext;

            this.clientInboundAddress = clientInboundAddress;
            this.clientOutboundAddress = clientOutboundAddress;
            this.serverInboundAddress = serverInboundAddress;
            this.serverOutboundAddress = serverOutboundAddress;
        }

        public void Listen()
        {
            this.EnsureIsRunning();
        }

        private void EnsureIsRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 0)
            {
                this.forwardRequestsTask = Task.Factory.StartNew(this.ForwardRequests, TaskCreationOptions.LongRunning);
                this.forwardResponsesTask = Task.Factory.StartNew(this.ForwardResponses, TaskCreationOptions.LongRunning);
            }
        }

        private void EnsureIsNotRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 0, 1) == 1)
            {
                Task.WaitAll(this.forwardRequestsTask, this.forwardResponsesTask);
                this.forwardRequestsTask = null;
                this.forwardResponsesTask = null;
            }
        }

        private void ForwardRequests()
        {
            using (var clientOutboundSocket = this.zmqContext.CreateNonBlockingReadonlySocket(SocketType.Router, TimeSpan.FromMilliseconds(100)))
            {
                //Bind client outbound
                clientOutboundSocket.Bind(this.clientOutboundAddress);

                using (var serverInboundSocket = this.zmqContext.CreateWriteonlySocket(SocketType.Dealer))
                {
                    //Bind server inbound
                    serverInboundSocket.Bind(this.serverInboundAddress);

                    //Forward messages from client inbound address to server outbound address
                    byte[] clientId;
                    byte[] frame;

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        clientId = clientOutboundSocket.Recv();
                        if (clientId != null && clientOutboundSocket.HasMoreToRecv())
                        {
                            serverInboundSocket.Send(clientId, hasMoreToSend: true);
                            frame = clientOutboundSocket.Recv();

                            while (clientOutboundSocket.HasMoreToRecv())
                            {
                                serverInboundSocket.Send(frame, hasMoreToSend: true);
                                frame = clientOutboundSocket.Recv();
                            }

                            serverInboundSocket.Send(frame);
                        }
                    }
                }
            }
        }

        private void ForwardResponses()
        {
            using (var serverOutboundSocket = this.zmqContext.CreateNonBlockingReadonlySocket(SocketType.Dealer, TimeSpan.FromMilliseconds(100)))
            {
                //Bind server outbound
                serverOutboundSocket.Bind(this.serverOutboundAddress);

                using (var clientInboundSocket = this.zmqContext.CreateWriteonlySocket(SocketType.Router))
                {
                    //Bind client inbound
                    clientInboundSocket.Bind(this.clientInboundAddress);

                    //Forward messages from server outbound address to client inbound address
                    byte[] clientId;
                    byte[] frame;

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        clientId = serverOutboundSocket.Recv();
                        if (clientId != null && serverOutboundSocket.HasMoreToRecv())
                        {
                            clientInboundSocket.Send(clientId, hasMoreToSend: true);
                            frame = serverOutboundSocket.Recv();

                            while (serverOutboundSocket.HasMoreToRecv())
                            {
                                clientInboundSocket.Send(frame, hasMoreToSend: true);
                                frame = serverOutboundSocket.Recv();
                            }

                            clientInboundSocket.Send(frame);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            this.EnsureIsNotRunning();
        }
    }
}
