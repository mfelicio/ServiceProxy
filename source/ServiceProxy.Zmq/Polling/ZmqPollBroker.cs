using Castle.Zmq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq.Polling
{
    public class ZmqPollBroker : IDisposable
    {
        private readonly string frontendAddress;
        private readonly string backendAddress;

        private readonly IZmqContext zmqContext;

        private long running;
        private volatile Task sendReceiveTask;

        public ZmqPollBroker(IZmqContext zmqContext,
                         string frontendAddress,
                         string backendAddress)
        {
            this.zmqContext = zmqContext;

            this.frontendAddress = frontendAddress;
            this.backendAddress = backendAddress;
        }

        public void Listen()
        {
            this.EnsureIsRunning();
        }

        private void EnsureIsRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 1, 0) == 0)
            {
                this.sendReceiveTask = Task.Factory.StartNew(this.SendReceive, TaskCreationOptions.LongRunning);
            }
        }

        private void EnsureIsNotRunning()
        {
            if (Interlocked.CompareExchange(ref this.running, 0, 1) == 1)
            {
                this.sendReceiveTask.Wait();
                this.sendReceiveTask = null;
            }
        }

        private void SendReceive()
        {
            using (var frontendSocket = this.zmqContext.CreateNonBlockingSocket(SocketType.Router, TimeSpan.FromMilliseconds(1)))
            {
                //Bind client outbound
                frontendSocket.Bind(this.frontendAddress);

                using (var backendSocket = this.zmqContext.CreateNonBlockingSocket(SocketType.Dealer, TimeSpan.FromMilliseconds(1)))
                {
                    //Bind server inbound
                    backendSocket.Bind(this.backendAddress);

                    var poller = new Castle.Zmq.Polling(PollingEvents.RecvReady, frontendSocket, backendSocket);

                    poller.RecvReady = s =>
                    {
                        if (s == frontendSocket)
                        {
                            //queue messages between frontend socket and backend socket
                            byte[] clientId;
                            byte[] frame;

                            clientId = frontendSocket.Recv();
                            if (clientId != null && frontendSocket.HasMoreToRecv())
                            {
                                backendSocket.Send(clientId, hasMoreToSend: true);
                                frame = frontendSocket.Recv();

                                while (frontendSocket.HasMoreToRecv())
                                {
                                    backendSocket.Send(frame, hasMoreToSend: true);
                                    frame = frontendSocket.Recv();
                                }

                                backendSocket.Send(frame);
                            }
                        }
                        else
                        {
                            //forward responses from backend socket to frontend socket
                            byte[] clientId;
                            byte[] frame;

                            clientId = backendSocket.Recv();
                            if (clientId != null && backendSocket.HasMoreToRecv())
                            {
                                frontendSocket.Send(clientId, hasMoreToSend: true);
                                frame = backendSocket.Recv();

                                while (backendSocket.HasMoreToRecv())
                                {
                                    frontendSocket.Send(frame, hasMoreToSend: true);
                                    frame = backendSocket.Recv();
                                }

                                frontendSocket.Send(frame);
                            }
                        }
                    };

                    var pollTimeout = 1; //ms

                    while (Interlocked.Read(ref this.running) == 1)
                    {
                        poller.Poll(pollTimeout);
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
