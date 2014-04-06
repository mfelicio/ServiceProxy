using ServiceContracts;
using ServiceProxy.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = new RedisClient(new RedisConnection("localhost"), "ThisIsTheClientQueue", "ThisIsTheServiceQueue"))
            {
                var clientFactory = new ServiceProxy.ServiceClientFactory(client);

                var fooService = clientFactory.CreateServiceClient<IFooService>();

                Console.WriteLine("Press ENTER for GetFooAndUpdate test");
                Console.ReadLine();

                GetFooAndUpdate(fooService);

                Console.WriteLine("Press ENTER for SimpleBenchmark test");
                Console.ReadLine();

                SimpleBenchmark(fooService).Wait();

                Console.WriteLine("Press ENTER to exit");
                Console.ReadLine();
            }
        }

        static void GetFooAndUpdate(IFooService fooService)
        {
            var foo = fooService.GetFoo(5);

            if (foo.Name == "Foo 5")
            {
                foo.Name = "Foo 1337";
                fooService.UpdateFoo(foo);

                var l33t = fooService.GetFoo(5);
                if (l33t.Name == "Foo 1337")
                {
                    Console.WriteLine("Successfully updated Foo 5 name to Foo 1337");
                }
            }
        }

        static async Task SimpleBenchmark(IFooService fooService)
        {
            var nCalls = 100 * 1000;

            var random = new Random();

            var tasksToWait = new ConcurrentBag<Task>();

            var sw = Stopwatch.StartNew();

            Parallel.For(0, nCalls, i =>
            {
                //gets a foo with id between 1 and 10, asynchronously
                tasksToWait.Add(
                    fooService.GetFooAsync(random.Next(1, 11)));
            });

            await Task.WhenAll(tasksToWait.ToArray());

            sw.Stop();

            Console.WriteLine("{0} calls completed in {1}", nCalls, sw.Elapsed);
            Console.WriteLine("Avg time per call: {0} ms", (double)sw.ElapsedMilliseconds / nCalls);
            Console.WriteLine("Requests per second: {0}", (double)nCalls / sw.Elapsed.TotalSeconds);
        }
    }
}
