using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ServiceProxy.Zmq.Polling
{
    public class ZmqPollBroker : IDisposable
    {
        private readonly string frontendAddress;
        private readonly string backendAddress;

        private readonly ZeroMQ.ZmqContext zmqContext;

        private long running;
        private volatile Task sendReceiveTask;

        public ZmqPollBroker(ZeroMQ.ZmqContext zmqContext,
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

        private void SendReceiveWithDevice()
        {
            using (var broker = new ZeroMQ.Devices.QueueDevice(this.zmqContext, this.frontendAddress, this.backendAddress, ZeroMQ.Devices.DeviceMode.Blocking))
            {
                try
                {
                    broker.Start();

                    //while (Interlocked.Read(ref this.running) == 1)
                    //{

                    //}
                }
                finally
                {
                    broker.Stop();
                }
            }
        }

        private void SendReceive()
        {
            using (var frontendSocket = this.zmqContext.CreateNonBlockingSocket(ZeroMQ.SocketType.ROUTER, TimeSpan.FromMilliseconds(1)))
            {
                //Bind client outbound
                frontendSocket.Bind(this.frontendAddress);

                using (var backendSocket = this.zmqContext.CreateNonBlockingSocket(ZeroMQ.SocketType.DEALER, TimeSpan.FromMilliseconds(1)))
                {
                    //Bind server inbound
                    backendSocket.Bind(this.backendAddress);

                    byte[] buffer = new byte[1024];

                    //queue messages between frontend socket and backend socket
                    frontendSocket.ReceiveReady += (s, e) =>
                    {
                        byte[] clientId;
                        byte[] frame;

                        int readBytes;
                        
                        clientId = frontendSocket.Receive(buffer, out readBytes);
                        if (readBytes > 0)
                        {
                            backendSocket.Send(clientId, readBytes, SocketFlags.SendMore);
                            frame = frontendSocket.Receive(buffer, out readBytes);

                            while (frontendSocket.ReceiveMore)
                            {
                                backendSocket.Send(frame, readBytes, SocketFlags.SendMore);
                                frame = frontendSocket.Receive(buffer, out readBytes);
                            }

                            backendSocket.Send(frame, readBytes, SocketFlags.None);
                        }
                    };

                    //forward responses from backend socket to frontend socket
                    backendSocket.ReceiveReady += (s, e) =>
                    {
                        byte[] clientId;
                        byte[] frame;

                        int readBytes;
                        
                        clientId = backendSocket.Receive(buffer, out readBytes);
                        if (readBytes > 0)
                        {
                            frontendSocket.Send(clientId, readBytes, SocketFlags.SendMore);
                            frame = backendSocket.Receive(buffer, out readBytes);

                            while (backendSocket.ReceiveMore)
                            {
                                frontendSocket.Send(frame, readBytes, SocketFlags.SendMore);
                                frame = backendSocket.Receive(buffer, out readBytes);
                            }

                            frontendSocket.Send(frame, readBytes, SocketFlags.None);
                        }
                    };

                    var poller = new ZeroMQ.Poller(new ZeroMQ.ZmqSocket[] { frontendSocket, backendSocket });
                    var pollTimeout = TimeSpan.FromMilliseconds(1);

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
