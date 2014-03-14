using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Internal
{
    internal class Service : IService
    {
        private readonly object serviceInstance;

        private readonly Dictionary<string, OperationInvoker> operationInvokers;

        public Service(Type serviceType, object serviceInstance)
        {
            this.serviceInstance = serviceInstance;

            var serviceMethods = ReflectionUtils.GetServiceOperations(serviceType);

            this.operationInvokers = serviceMethods.Where(m => m.IsSyncOperation())
                                                   .Select(m => new SynchronousOperationInvoker(m) as OperationInvoker)
                                                   .Union(
                                                        serviceMethods.Where(m => m.IsTaskBasedAsyncOperation())
                                                                      .Select(m => new TaskBasedOperationInvoker(m) as OperationInvoker)
                                                   )
                                                   .Union(
                                                        serviceMethods.Where(m => m.IsAsyncResultBasedOperation())
                                                                      .Select(m => new AsyncResultBasedOperation(m) as OperationInvoker)
                                                   )
                                                   .ToDictionary(op => op.Name, op => op);
        }

        public Task<ResponseData> Process(RequestData requestData)
        {
            var op = this.operationInvokers[requestData.Operation];

            return op.InvokeAsync(this.serviceInstance, requestData.Arguments)
                     .ContinueWith(t =>
                      {
                          if (t.Exception == null)
                          {
                              return new ResponseData(t.Result);
                          }
                          else
                          {
                              return new ResponseData(t.Exception.InnerException);
                          }
                      });
        }
    }
}
