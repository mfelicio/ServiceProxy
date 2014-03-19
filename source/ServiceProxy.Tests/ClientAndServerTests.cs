using Moq;
using NUnit.Framework;
using ServiceProxy.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Tests
{
    public class ClientAndServerTests
    {
        private readonly IDependencyResolver resolver = new DependencyResolver();

        [SetUp]
        public void Setup()
        {

        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public async void RequestGetsResponseFromServer()
        {
            var clientFactory = new ServiceClientFactory(new SimpleClient(new ServiceFactory(this.resolver)));

            var serviceClient = clientFactory.CreateServiceClient<ITestService2>();

            //Synchronous
            Assert.That(serviceClient.GetPerson(1), Is.Not.Null);

            //Asynchronous task based
            var persons = await serviceClient.ListPersonsAsync(5);

            Assert.That(persons, Is.Not.Null);
            Assert.AreEqual(5, persons.Count());

            //Asynchronous IAsyncResult based , awaiting with Task
            var person = await Task.Factory.FromAsync<int, Person>(serviceClient.BeginGetPerson, serviceClient.EndGetPerson, 1, null);
            Assert.That(person, Is.Not.Null);

            var nullCollection = await serviceClient.ListPersonsAsync(-1);
            Assert.IsNull(nullCollection);

            var nullObject = serviceClient.GetPerson(-1);
            Assert.IsNull(nullObject);
        }

        [Test]
        public void RequestGetsExceptionFromServer()
        {
            var clientFactory = new ServiceClientFactory(new SimpleClient(new ServiceFactory(this.resolver)));

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
