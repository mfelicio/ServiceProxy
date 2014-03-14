using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Internal
{
    internal class ServiceClientInterceptor : IInterceptor
    {
        private readonly Type contractType;
        private readonly IClient client;
        private readonly Dictionary<string, OperationInterceptor> operationInvokers;

        public ServiceClientInterceptor(Type contractType, IClient client)
        {
            this.contractType = contractType;
            this.client = client;
            this.operationInvokers = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                 .ToDictionary(m => m.Name, this.CreateInvoker);
        }

        private OperationInterceptor CreateInvoker(MethodInfo method)
        {
            var parameters = method.GetParameters();
            //supports IAsyncResult Begin/End, Task and synchronous

            //check if asynchronous
            if (typeof(IAsyncResult).IsAssignableFrom(method.ReturnType))
            {
                //check if task
                if (typeof(Task).IsAssignableFrom(method.ReturnType))
                {
                    return new TaskOperationInterceptor(this.contractType, method, this.client);
                }
                else
                {
                    //validate Begin APM signature
                    if (parameters.Length >= 2
                        && parameters[parameters.Length - 2].ParameterType == typeof(AsyncCallback)
                        && parameters[parameters.Length - 1].ParameterType == typeof(object)
                        && method.Name.StartsWith("Begin"))
                    {
                        return new BeginAsyncResultOperationInterceptor(this.contractType, method, this.client);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Method {0}.{1} isn't compliant with IAsyncResult APM signature", this.contractType.FullName, method.Name));
                    }
                }
            }
            else
            {
                //check if is an End APM signature
                if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(IAsyncResult)
                    && method.Name.StartsWith("End"))
                {
                    return new EndAsyncResultOperationInterceptor(this.contractType, method, this.client);
                }
                else
                {
                    //synchronous invoker
                    return new SynchronousOperationInterceptor(this.contractType, method, this.client);
                }
            }
        }

        public void Intercept(IInvocation invocation)
        {
            var operation = invocation.Method.Name;
            var invoker = operationInvokers[operation];

            invoker.Invoke(invocation);
        }

    }
}
