ServiceProxy.Zmq
============

ServiceProxy.Zmq is a scalable request/reply messaging framework built with ZeroMQ that supports service contracts using ServiceProxy.

## Getting started

The quickest way to get started is to use the [Nuget package][serviceproxy.zmq-nuget].

### Example from a unit test in ServiceProxy.Zmq.Tests

```c#
public async void TestSendAndReceive()
{
    var resolver = new DependencyResolver();

    using (var broker = new ZmqBroker(this.zmqContext, ClientInboundAddress, ClientOutboundAddress, ServerInboundAddress, ServerOutboundAddress))
    {
        broker.Listen();

        using (var server = new ZmqServer(this.zmqContext, ServerInboundAddress, ServerOutboundAddress, new ServiceFactory(resolver)))
        {
            server.Listen();

            using (var client = new ZmqClient(this.zmqContext, ClientInboundAddress, ClientOutboundAddress))
            {
                var clientFactory = new ServiceClientFactory(client);

                var serviceClient = clientFactory.CreateServiceClient<ITestService2>();

                Assert.That(serviceClient.GetPerson(1), Is.Not.Null);

                var persons = await serviceClient.ListPersonsAsync(5);
                Assert.That(persons, Is.Not.Null);
                Assert.AreEqual(5, persons.Count());
            }
        }
    }
}
```

The ZmqClient, ZmqBroker and ZmqServer instances should be created in different processes/machines. It supports load balancing by having multiple ZmqServer instances connected to the ZmqBroker.

## Dependencies

ServiceProxy.Zmq uses the [clrzmq][clrzmq-github] ZeroMQ binding for .NET. This version uses libzmq 3.2.2-rc2.

[serviceproxy.zmq-nuget]: http://www.nuget.org/packages/ServiceProxy.Zmq
[clrzmq-github]: https://github.com/zeromq/clrzmq

