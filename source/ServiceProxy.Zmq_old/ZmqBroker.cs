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

        private readonly ZMQ.Context zmqContext;

        private long running;
        private volatile Task forwardRequestsTask;
        private volatile Task forwardResponsesTask;

        public ZmqBroker(ZMQ.Context zmqContext,
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
            using (var clientOutboundSocket = this.zmqContext.Socket(ZMQ.SocketType.ROUTER))
            {
                //Bind client outbound
                clientOutboundSocket.Bind(this.clientOutboundAddress);
                
                using (var serverInboundSocket = this.zmqContext.Socket(ZMQ.SocketType.DEALER))
                {
                    //Bind server inbound
                    serverInboundSocket.Bind(this.serverInboundAddress);

                    //Forward messages from client inbound address to server outbound address
                    byte[] clientId;
                    byte[] frame;
                    var timeout = 100;

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        clientId = clientOutboundSocket.Recv(timeout);
                        if (clientId != null)
                        {
                            serverInboundSocket.SendMore(clientId);
                            frame = clientOutboundSocket.Recv();

                            while (clientOutboundSocket.RcvMore)
                            {
                                serverInboundSocket.SendMore(frame);
                                frame = clientOutboundSocket.Recv();
                            }

                            serverInboundSocket.Send(frame, ZMQ.SendRecvOpt.NOBLOCK);
                        }
                    }
                }
            }
        }

        private void ForwardResponses()
        {
            using (var serverOutboundSocket = this.zmqContext.Socket(ZMQ.SocketType.DEALER))
            {
                //Bind server outbound
                serverOutboundSocket.Bind(this.serverOutboundAddress);

                using (var clientInboundSocket = this.zmqContext.Socket(ZMQ.SocketType.ROUTER))
                {
                    //Bind client inbound
                    clientInboundSocket.Bind(this.clientInboundAddress);

                    //Forward messages from server outbound address to client inbound address
                    byte[] clientId;
                    byte[] frame;
                    var timeout = 100;

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        clientId = serverOutboundSocket.Recv(timeout);
                        if (clientId != null)
                        {
                            clientInboundSocket.SendMore(clientId);
                            frame = serverOutboundSocket.Recv();

                            while (serverOutboundSocket.RcvMore)
                            {
                                clientInboundSocket.SendMore(frame);
                                frame = serverOutboundSocket.Recv();
                            }

                            clientInboundSocket.Send(frame, ZMQ.SendRecvOpt.NOBLOCK);
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
