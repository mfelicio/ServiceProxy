using Autofac;
using Autofac.Extras.DynamicProxy2;
using Castle.DynamicProxy;
using MyApp.Services;
using MyApp.Services.InMemory;
using ServiceProxy;
using ServiceProxy.Zmq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverInboundAddr = "tcp://localhost:8003";
            var serverOutboundAddr = "tcp://localhost:8004";

            var builder = new ContainerBuilder();

            builder.RegisterType<LogInterceptor>().AsSelf().As<IInterceptor>();

            builder.RegisterType<CatalogService>().As<ICatalogService>()
                        .EnableInterfaceInterceptors()
                        .InterceptedBy(typeof(LogInterceptor)); ;
            builder.RegisterType<FooService>().As<IFooService>()
                        .EnableInterfaceInterceptors()
                        .InterceptedBy(typeof(LogInterceptor));

            builder.RegisterType<AutofacDependencyResolver>().As<IDependencyResolver>();
            builder.RegisterType<ServiceFactory>().As<IServiceFactory>();

            var container = builder.Build();

            ZmqServer server = null;
            try
            {
                server = new ZmqServer(new ZMQ.Context(), serverInboundAddr, serverOutboundAddr, container.Resolve<IServiceFactory>());
                server.Listen();

                Console.WriteLine("Press enter to quit");
                Console.ReadLine();
            }
            finally
            {
                if (server != null)
                {
                    server.Dispose();
                }
            }
        }
    }

    public class AutofacDependencyResolver : IDependencyResolver
    {
        private readonly ILifetimeScope container;

        public AutofacDependencyResolver(ILifetimeScope container)
        {
            this.container = container;
        }

        public object Resolve(Type type)
        {
            return this.container.Resolve(type);
        }
    }

}
