using ServiceProxy.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy
{
    public class ServiceFactory : IServiceFactory
    {
        private readonly IDependencyResolver resolver;
        //using Lazy with ConcurrentDictionary ensures that the CreateNewService method will only be invoked once per serviceType
        private readonly ConcurrentDictionary<Type, Lazy<IService>> services;
        
        private readonly ConcurrentDictionary<string, Type> serviceNamesMap;

        public ServiceFactory(IDependencyResolver resolver)
        {
            this.resolver = resolver;
            this.services = new ConcurrentDictionary<Type, Lazy<IService>>();
            this.serviceNamesMap = new ConcurrentDictionary<string, Type>();
        }

        public IService CreateService<TService>()
            where TService : class
        {
            return this.CreateService(typeof(TService));
        }

        public IService CreateService(string serviceName)
        {
            var serviceType = this.serviceNamesMap.GetOrAdd(serviceName, name => Type.GetType(name));

            return this.CreateService(serviceType);
        }

        public IService CreateService(Type serviceType)
        {
            var service = this.services.GetOrAdd(serviceType, 
                                                 type => new Lazy<IService>(() => this.CreateNewService(type), LazyThreadSafetyMode.ExecutionAndPublication));
            return service.Value;
        }

        private IService CreateNewService(Type serviceType)
        {
            return new Service(serviceType, this.resolver.Resolve(serviceType));
        }
    }
}
