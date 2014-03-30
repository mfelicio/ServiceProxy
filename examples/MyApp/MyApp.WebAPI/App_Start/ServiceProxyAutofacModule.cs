using Autofac;
using MyApp.Services;
using ServiceProxy;
using ServiceProxy.Zmq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyApp.WebAPI
{
    public class ServiceProxyAutofacModule : Module
    {
        protected override void Load(Autofac.ContainerBuilder builder)
        {
            builder.RegisterType<ServiceClientFactory>().As<IServiceClientFactory>().SingleInstance();

            builder.RegisterInstance(new ZmqClient(new ZMQ.Context(), "tcp://localhost:8001", "tcp://localhost:8002"))
                    .As<IClient>().SingleInstance();

            builder.Register<ICatalogService>(ctx => ctx.Resolve<IServiceClientFactory>().CreateServiceClient<ICatalogService>(15000));
            builder.Register<IFooService>(ctx => ctx.Resolve<IServiceClientFactory>().CreateServiceClient<IFooService>(15000));
        }
    }
}