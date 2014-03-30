using ServiceProxy.Zmq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Broker
{
    class Program
    {
        static void Main(string[] args)
        {
            //Note: 0MQ tcp sockets only accept IPv4 addresses, wild card or interface-name for the Bind operation.
            var clientInboundAddr = "tcp://*:8001";
            var clientOutboundAddr = "tcp://*:8002";
            var serverInboundAddr = "tcp://*:8003";
            var serverOutboundAddr = "tcp://*:8004";

            ZmqBroker broker = null;

            try
            {
                broker = new ZmqBroker(new ZMQ.Context(), clientInboundAddr, clientOutboundAddr, serverInboundAddr, serverOutboundAddr);
                broker.Listen();

                Console.WriteLine("Press enter to quit");
                Console.ReadLine();
            }
            finally
            {
                if (broker != null)
                {
                    broker.Dispose();
                }
            }
        }
    }
}
