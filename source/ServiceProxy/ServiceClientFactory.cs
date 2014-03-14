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

        public TService CreateServiceClient<TService>()
            where TService : class
        {
            var serviceClient = (TService)this.CreateServiceClient(typeof(TService));
            return serviceClient;
        }

        public object CreateServiceClient(Type serviceType)
        {
            var client = this.clients.GetOrAdd(serviceType, 
                                               type => new Lazy<object>(() => this.GenerateClientProxy(type), LazyThreadSafetyMode.ExecutionAndPublication));
            return client.Value;
        }

        private object GenerateClientProxy(Type serviceType)
        {
            var interceptor = new ServiceClientInterceptor(serviceType, this.client);
            var proxy = this.proxyGenerator.CreateInterfaceProxyWithoutTarget(serviceType, interceptor);
            return proxy;
        }
    }

    

    

}
