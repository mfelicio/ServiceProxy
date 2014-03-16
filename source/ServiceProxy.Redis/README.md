ServiceProxy.Redis
============

ServiceProxy.Redis is a simple request/reply messaging framework built on Redis queues that supports service contracts using ServiceProxy.

## Getting started

The quickest way to get started is to use the [Nuget package][serviceproxy.zmq-nuget].

### Example from a unit test in ServiceProxy.Redis.Tests

```c#
public async void TestSendAndReceive()
{
    var resolver = new DependencyResolver();

    using (var server = new RedisServer(new RedisDuplexConnection(RedisHost, RedisPort, RedisPassword), ServerQueue, new ServiceFactory(resolver)))
    {
        server.Listen();

        using (var client = new RedisClient(new RedisDuplexConnection(RedisHost, RedisPort, RedisPassword), ClientQueue, ServerQueue))
        {
            var clientFactory = new ServiceProxy.ServiceClientFactory(client);

            var serviceClient = clientFactory.CreateServiceClient<ITestService2>();

            Assert.That(serviceClient.GetPerson(1), Is.Not.Null);

            var persons = await serviceClient.ListPersonsAsync(5);
            Assert.That(persons, Is.Not.Null);
            Assert.AreEqual(5, persons.Count());
        }
    }
}
```

The client and server should be created in different processes/machines, using Redis as a middleware and ServiceProxy to support service contracts.

It supports load balancing by having multiple servers listening on the same Redis queue.

## Dependencies

ServiceProxy.Redis uses the [Booksleeve][booksleeve-home] Redis client library, which has a Task based asynchronous API for all redis commands, integrating nicely with ServiceProxy.

[serviceproxy.redis-nuget]: http://www.nuget.org/packages/ServiceProxy.Redis
[booksleeve-home]: https://code.google.com/p/booksleeve/