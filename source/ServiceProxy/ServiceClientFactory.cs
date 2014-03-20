using Castle.DynamicProxy;
using ServiceProxy.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy
{
    public class ServiceClientFactory : IServiceClientFactory
    {
        private readonly ProxyGenerator proxyGenerator;
        //using Lazy with ConcurrentDictionary ensures that the GenerateClientProxy method will only be invoked once per serviceType
        private readonly ConcurrentDictionary<Type, Lazy<object>> clients;

        private readonly IClient client;

        public ServiceClientFactory(IClient client)
        {
            this.proxyGenerator = new ProxyGenerator();
            this.clients = new ConcurrentDictionary<Type, Lazy<object>>();

            this.client = client;
        }

        public TService CreateServiceClient<TService>(int? timeout = null)
            where TService : class
        {
            var serviceClient = (TService)this.CreateServiceClient(typeof(TService), timeout);
            return serviceClient;
        }

        public object CreateServiceClient(Type serviceType, int? timeout = null)
        {
            var client = this.clients.GetOrAdd(serviceType, 
                                               type => new Lazy<object>(() => this.GenerateClientProxy(type, timeout), LazyThreadSafetyMode.ExecutionAndPublication));
            return client.Value;
        }

        private object GenerateClientProxy(Type serviceType, int? timeout)
        {
            IClient clientInstance = timeout == null ? this.client : new TimeoutClient(this.client, TimeSpan.FromMilliseconds(timeout.Value));

            var interceptor = new ServiceClientInterceptor(serviceType, clientInstance);
            var proxy = this.proxyGenerator.CreateInterfaceProxyWithoutTarget(serviceType, interceptor);
            return proxy;
        }
    }

    

    

}
