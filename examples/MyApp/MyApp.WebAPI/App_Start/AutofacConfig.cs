using System;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;
using System.Web.Routing;
using Swagger.Net;
using Autofac;
using Autofac.Integration.WebApi;

[assembly: WebActivator.PreApplicationStartMethod(typeof(MyApp.WebAPI.AutofacConfig), "PreStart")]
[assembly: WebActivator.PostApplicationStartMethod(typeof(MyApp.WebAPI.AutofacConfig), "PostStart")]

namespace MyApp.WebAPI
{
    public static class AutofacConfig
    {
        public static void PreStart()
        {
        }

        public static void PostStart()
        {
            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(typeof(WebApiApplication).Assembly);

            builder.RegisterModule(new ServiceProxyAutofacModule());

            var container = builder.Build();

            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}