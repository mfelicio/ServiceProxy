using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Tests.Stubs
{
    class SimpleClient : IClient
    {
        private readonly IServiceFactory serviceFactory;

        public SimpleClient(IServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        public Task<ResponseData> Request(RequestData request, CancellationToken token)
        {
            var svc = this.serviceFactory.CreateService(request.Service);
            var responseTask = svc.Process(request);

            return responseTask;
        }
    }

}
