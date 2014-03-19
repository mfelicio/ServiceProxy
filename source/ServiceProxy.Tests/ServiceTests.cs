using Moq;
using NUnit.Framework;
using ServiceProxy.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Tests
{
    [TestFixture]
    public class ServiceTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void CanCreateService()
        {
            var factory = new ServiceFactory(new Mock<IDependencyResolver>().Object);

            var service = factory.CreateService<ITestService2>();
            Assert.That(service, Is.Not.Null);
        }

        [TestCase(typeof(ITestService2), "ListPersons", new object[] { 3 })]
        [TestCase(typeof(ITestService2), "ListPersonsAsync", new object[] { 3 })]
        [TestCase(typeof(ITestService2), "BeginListPersons", new object[] { 3 })]
        public async void ProcessRequest_IEnumerable(Type serviceType, string operation, object[] arguments)
        {
            var response = await ProcessRequestInternal(serviceType, operation, arguments);

            Assert.IsNotNull(response.Data);
            Assert.IsTrue(typeof(IEnumerable<Person>).IsAssignableFrom(response.Data.GetType()));
        }

        [TestCase(typeof(ITestService2), "GetPerson", new object[] { 3 })]
        [TestCase(typeof(ITestService2), "GetPersonAsync", new object[] { 3 })]
        [TestCase(typeof(ITestService2), "BeginGetPerson", new object[] { 3 })]
        public async void ProcessRequest_ComplexType(Type serviceType, string operation, object[] arguments)
        {
            var responseData = await ProcessRequestInternal(serviceType, operation, arguments);

            Assert.IsNotNull(responseData.Data);
            Assert.IsInstanceOf<Person>(responseData.Data);
        }

        [TestCase(typeof(ITestService), "Sum", new object[] { 10, 5 })]
        [TestCase(typeof(ITestService), "Concatenate", new object[] { new string[] { "a", "b", "c" } })]
        [TestCase(typeof(ITestService), "GetDate", new object[0])]
        public async void ProcessRequest_SimpleArguments(Type serviceType, string operation, object[] arguments)
        {
            var responseData = await ProcessRequestInternal(serviceType, operation, arguments);

            Assert.IsNull(responseData.Exception);
            Assert.IsNotNull(responseData.Data);
        }

        [TestCase(typeof(ITestService2), "ListPersons", new object[] { -1 })]
        [TestCase(typeof(ITestService2), "ListPersonsAsync", new object[] { -1 })]
        [TestCase(typeof(ITestService2), "BeginListPersons", new object[] { -1 })]
        [TestCase(typeof(ITestService2), "GetPerson", new object[] { -1 })]
        [TestCase(typeof(ITestService2), "GetPersonAsync", new object[] { -1 })]
        [TestCase(typeof(ITestService2), "BeginGetPerson", new object[] { -1 })]
        public async void ProcessRequest_NullResults(Type serviceType, string operation, object[] arguments)
        {
            var response = await ProcessRequestInternal(serviceType, operation, arguments);

            Assert.IsNull(response.Exception);
            Assert.IsNull(response.Data);
        }

        private Task<ResponseData> ProcessRequestInternal(Type serviceType, string operation, object[] arguments)
        {
            var factory = new ServiceFactory(new DependencyResolver());
            var service = factory.CreateService(serviceType);

            var requestData = new RequestData(serviceType.FullName, operation, arguments);

            return service.Process(requestData);
        }
    }
}
