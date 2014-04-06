using NUnit.Framework;
using ServiceProxy.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Redis.Tests
{
    [TestFixture]
    public class RedisClientAndServerTests
    {
        private readonly string ClientQueue = "cQueue";
        private readonly string ServerQueue = "sQueue";

        private readonly string RedisHost = "localhost";
        private readonly int RedisPort = 6379;
        private readonly string RedisPassword = null;

        private Process redisServerProcess;

        [TestFixtureSetUp]
        public void StartRedis()
        {
            try
            {
                this.redisServerProcess = Process.Start(new ProcessStartInfo("redis-server.lnk"));
            }
            catch (Exception ex)
            {
                Assert.Ignore("Could not start redis-server. Check redis-server.lnk path. Error: {0}", ex.Message);
            }
        }

        [TestFixtureTearDown]
        public void StopRedis()
        {
            if (this.redisServerProcess != null)
            {
                this.redisServerProcess.Kill();
                this.redisServerProcess = null;
            }
        }

        [Test]
        public async void TestSendAndReceive()
        {
            var resolver = new DependencyResolver();

            using (var server = new RedisServer(new RedisConnection(RedisHost, RedisPort, RedisPassword), ServerQueue, new ServiceFactory(resolver)))
            {
                server.Listen();

                using (var client = new RedisClient(new RedisConnection(RedisHost, RedisPort, RedisPassword), ClientQueue, ServerQueue))
                {
                    var clientFactory = new ServiceProxy.ServiceClientFactory(client);

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

        [Test]
        public async void TestSendAndReceiveExceptions()
        {
            var resolver = new DependencyResolver();

            using (var server = new RedisServer(new RedisConnection(RedisHost, RedisPort, RedisPassword), ServerQueue, new ServiceFactory(resolver)))
            {
                server.Listen();

                using (var client = new RedisClient(new RedisConnection(RedisHost, RedisPort, RedisPassword), ClientQueue, ServerQueue))
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

                    //Timeout exceptions
                    var factoryWithTimeout = new ServiceClientFactory(client);
                    var serviceClientWithTimeout = factoryWithTimeout.CreateServiceClient<ITestService>(50); //50ms

                    Assert.Throws<TimeoutException>(async () => await serviceClientWithTimeout.ReplyAfter(1000));
                }
            }
        }

        [Test]
        [TestCase(1, 1000)]
        [TestCase(2, 10000)]
        [TestCase(2, 100000)]
        //[TestCase(5, 1000000)]
        public async void TestLoadBalancing(int nServers, int nMsgs)
        {
            var resolver = new DependencyResolver();

            var servers = Enumerable.Range(0, nServers)
                                    .Select(i => new RedisServer(new RedisConnection(RedisHost, RedisPort, RedisPassword), ServerQueue, new ServiceFactory(resolver)))
                                    .ToArray();

            try
            {
                foreach (var server in servers) server.Listen();

                using (var client = new RedisClient(new RedisConnection(RedisHost, RedisPort, RedisPassword), ClientQueue, ServerQueue))
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
