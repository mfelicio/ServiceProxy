using NUnit.Framework;
using ServiceProxy.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq.Tests
{
    [TestFixture]
    public class ZmqClientAndServerTests
    {
        const string ClientInboundAddress = "tcp://127.0.0.1:8001";
        const string ClientOutboundAddress = "tcp://127.0.0.1:8002";
        const string ServerInboundAddress = "tcp://127.0.0.1:8003";
        const string ServerOutboundAddress = "tcp://127.0.0.1:8004";

        private ZMQ.Context zmqContext;

        [SetUp]
        public void Setup()
        {
            this.zmqContext = new ZMQ.Context(4);
        }

        [TearDown]
        public void TearDown()
        {
            this.zmqContext.Dispose();
        }

        [Test]
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

                        var nullCollection = await serviceClient.ListPersonsAsync(-1);
                        Assert.IsNull(nullCollection);

                        var nullObject = serviceClient.GetPerson(-1);
                        Assert.IsNull(nullObject);
                    }
                }
            }
        }

        [Test]
        public async void TestSendAndReceiveExceptions()
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

                        var serviceClient = clientFactory.CreateServiceClient<ITestService>();

                        //Synchronous
                        var err = Assert.Catch(async () => await serviceClient.FailAsync());
                        Assert.IsNotNull(err);
                        Assert.IsNotInstanceOf<AggregateException>(err);

                        //Asynchronous task based
                        err = Assert.Catch(() => serviceClient.Fail());
                        Assert.IsNotNull(err);
                        Assert.IsNotInstanceOf<AggregateException>(err);

                        //Asynchronous IAsyncResult based , awaiting with Task
                        err = Assert.Catch(async () => await Task.Factory.FromAsync(serviceClient.BeginFail, serviceClient.EndFail, null));
                        Assert.IsNotNull(err);
                        Assert.IsNotInstanceOf<AggregateException>(err);
                    }
                }
            }
        }

        [Test]
        [TestCase(1, 1000)]
        [TestCase(2, 10000)]
        //[TestCase(4, 100000)]
        //[TestCase(5, 1000000)]
        public async void TestLoadBalancing(int nServers, int nMsgs)
        {
            var resolver = new DependencyResolver();

            using (var broker = new ZmqBroker(this.zmqContext, ClientInboundAddress, ClientOutboundAddress, ServerInboundAddress, ServerOutboundAddress))
            {
                broker.Listen();

                var servers = Enumerable.Range(0, nServers)
                                        .Select(i => new ZmqServer(this.zmqContext, ServerInboundAddress, ServerOutboundAddress, new ServiceFactory(resolver)))
                                        .ToArray();

                try
                {
                    foreach (var server in servers) server.Listen();

                    using (var client = new ZmqClient(this.zmqContext, ClientInboundAddress, ClientOutboundAddress))
                    {
                        var clientFactory = new ServiceClientFactory(client);

                        var serviceClient = clientFactory.CreateServiceClient<ITestService2>();

                        var tasks = Enumerable.Range(0, nMsgs)
                            //.Select(i => serviceClient.SumAsync(5, 15))
                                              .Select(i => serviceClient.ListPersonsAsync(7))
                                              .ToArray();

                        await Task.WhenAll(tasks);

                        Assert.True(tasks.All(t => t.Result.Count() == 7));

                    }

                }
                finally
                {
                    foreach (var server in servers) server.Dispose();
                }

            }
        }
    }
}
