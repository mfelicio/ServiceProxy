using ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class FooService : IFooService
    {
        public Foo GetFoo(int id)
        {
            return FooDb.Get(id);
        }

        public IEnumerable<Foo> ListFoos()
        {
            return FooDb.All();
        }

        public void UpdateFoo(Foo foo)
        {
            FooDb.Update(foo);
        }

        public Task<Foo> GetFooAsync(int id)
        {
            return Task.FromResult(this.GetFoo(id));
        }

        public Task<IEnumerable<Foo>> ListFoosAsync()
        {
            return Task.FromResult(this.ListFoos());
        }

        public async Task UpdateFooAsync(Foo foo)
        {
            await Task.Delay(100); //lets introduce some latency

            this.UpdateFoo(foo);
        }
    }

    static class FooDb
    {
        static readonly ConcurrentDictionary<int, Foo> foos;

        static FooDb()
        {
            var data = Enumerable.Range(1, 10)
                                 .Select(i => new Foo { Id = i, Name = string.Format("Foo {0}", i) });

            foos = new ConcurrentDictionary<int, Foo>(data.ToDictionary(f => f.Id, f => f));
        }

        public static Foo Get(int id)
        {
            Foo foo;
            foos.TryGetValue(id, out foo);
            return foo;
        }

        public static void Update(Foo foo)
        {
            foos.AddOrUpdate(foo.Id, foo, (id, old) => foo);
        }

        public static IEnumerable<Foo> All()
        {
            return foos.Values.ToArray();
        }
    }
}
