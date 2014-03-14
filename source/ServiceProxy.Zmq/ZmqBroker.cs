using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ServiceProxy.Zmq
{
    public class ZmqBroker : IDisposable
    {
        private readonly string clientInboundAddress;
        private readonly string clientOutboundAddress;
        private readonly string serverInboundAddress;
        private readonly string serverOutboundAddress;

        private readonly ZeroMQ.ZmqContext zmqContext;

        private long running;
        private volatile Task forwardRequestsTask;
        private volatile Task forwardResponsesTask;

        public ZmqBroker(ZeroMQ.ZmqContext zmqContext,
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
            using (var clientOutboundSocket = this.zmqContext.CreateNonBlockingReadonlySocket(ZeroMQ.SocketType.ROUTER, TimeSpan.FromMilliseconds(100)))
            {
                //Bind client outbound
                clientOutboundSocket.Bind(this.clientOutboundAddress);

                using (var serverInboundSocket = this.zmqContext.CreateWriteonlySocket(ZeroMQ.SocketType.DEALER))
                {
                    //Bind server inbound
                    serverInboundSocket.Bind(this.serverInboundAddress);

                    //Forward messages from client inbound address to server outbound address
                    byte[] clientId;
                    byte[] frame;

                    byte[] buffer = new byte[1024];
                    int readBytes;

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        clientId = clientOutboundSocket.Receive(buffer, out readBytes);
                        if (readBytes > 0)
                        {
                            serverInboundSocket.Send(clientId, readBytes, SocketFlags.SendMore);
                            frame = clientOutboundSocket.Receive(buffer, out readBytes);

                            while (clientOutboundSocket.ReceiveMore)
                            {
                                serverInboundSocket.Send(frame, readBytes, SocketFlags.SendMore);
                                frame = clientOutboundSocket.Receive(buffer, out readBytes);
                            }

                            serverInboundSocket.Send(frame, readBytes, SocketFlags.None);
                        }
                    }
                }
            }
        }

        private void ForwardResponses()
        {
            using (var serverOutboundSocket = this.zmqContext.CreateNonBlockingReadonlySocket(ZeroMQ.SocketType.DEALER, TimeSpan.FromMilliseconds(100)))
            {
                //Bind server outbound
                serverOutboundSocket.Bind(this.serverOutboundAddress);

                using (var clientInboundSocket = this.zmqContext.CreateWriteonlySocket(ZeroMQ.SocketType.ROUTER))
                {
                    //Bind client inbound
                    clientInboundSocket.Bind(this.clientInboundAddress);

                    //Forward messages from server outbound address to client inbound address
                    byte[] clientId;
                    byte[] frame;

                    byte[] buffer = new byte[1024];
                    int readBytes;

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        clientId = serverOutboundSocket.Receive(buffer, out readBytes);
                        if (readBytes > 0)
                        {
                            clientInboundSocket.Send(clientId, readBytes, SocketFlags.SendMore);
                            frame = serverOutboundSocket.Receive(buffer, out readBytes);

                            while (serverOutboundSocket.ReceiveMore)
                            {
                                clientInboundSocket.Send(frame, readBytes, SocketFlags.SendMore);
                                frame = serverOutboundSocket.Receive(buffer, out readBytes);
                            }

                            clientInboundSocket.Send(frame, readBytes, SocketFlags.None);
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
