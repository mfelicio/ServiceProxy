using ServiceProxy.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceFactory = new ServiceProxy.ServiceFactory(new SimpleDependencyResolver());

            using (var server = new RedisServer(new RedisDuplexConnection("localhost"), "ThisIsTheServiceQueue", serviceFactory))
            {
                server.Listen();

                Console.WriteLine("Press ENTER to close");
                Console.ReadLine();
            }
        }
    }

    //This should be an adapter for an IoC container
    class SimpleDependencyResolver : ServiceProxy.IDependencyResolver
    {
        public object Resolve(Type type)
        {
            return new FooService();
        }
    }
}
