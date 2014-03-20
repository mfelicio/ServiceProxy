using Moq;
using NUnit.Framework;
using ServiceProxy.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Tests
{
    [TestFixture]
    public class ClientTests
    {
        [SetUp]
        public void Setup()
        {

        }

        private void SetupClient(Mock<IClient> clientMock, int? timeToReply = null)
        {
            clientMock.Setup(c => c.Request(It.IsAny<RequestData>(), It.IsAny<CancellationToken>()))
                      .Returns(
                        (RequestData request, CancellationToken token) =>
                        {
                            Task<ResponseData> responseTask;

                            if (request.Operation == "Sum")
                            {
                                var data = new TestService().Sum((int)request.Arguments[0], (int)request.Arguments[1]);
                                var response = new ResponseData(data);
                                responseTask = Task.FromResult(response);
                            }
                            else if (request.Operation == "Concatenate")
                            {
                                var dataTask = new TestService().Concatenate(request.Arguments[0] as string[]);
                                responseTask = dataTask.ContinueWith(t => new ResponseData(t.Result));
                            }
                            else
                            {
                                throw new NotSupportedException("Only sum and Concatenate are supported in this mock");
                            }

                            if (timeToReply.HasValue)
                            {
                                return Task.Delay(timeToReply.Value).ContinueWith(t => responseTask).Unwrap();
                            }
                            else
                            {
                                return responseTask;
                            }
                        });
        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void CanCreateClient()
        {
            var factory = new ServiceClientFactory(new Mock<IClient>().Object);

            var serviceClient = factory.CreateServiceClient<ITestService>();
            Assert.That(serviceClient, Is.Not.Null);
        }

        [Test]
        public void CanUseClientWithNonComplexTypes()
        {
            var clientMock = new Mock<IClient>();
            this.SetupClient(clientMock);

            var factory = new ServiceClientFactory(clientMock.Object);
            var serviceClient = factory.CreateServiceClient<ITestService>();

            var sum = serviceClient.Sum(10, 5);
            Assert.That(sum, Is.EqualTo(15));

            var concatenated = serviceClient.Concatenate("10", "01");
            Assert.That(concatenated.Result, Is.EqualTo("1001"));

        }

        [Test]
        public void CanHandleTimeouts()
        {
            var clientMock = new Mock<IClient>();
            this.SetupClient(clientMock, 100); //takes 100ms to reply

            var factory = new ServiceClientFactory(clientMock.Object);
            var serviceClient = factory.CreateServiceClient<ITestService>(50); //50ms timeout

            Assert.Throws<TimeoutException>(() => serviceClient.Sum(10, 5));
        }
    }
}
