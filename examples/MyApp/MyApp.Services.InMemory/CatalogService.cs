using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Services.InMemory
{
    public class CatalogService : ICatalogService
    {

        public IEnumerable<string> GetCatalogNames()
        {
            return Data.Catalogs.Keys;
        }

        public Task<IEnumerable<string>> GetCatalogNamesAsync()
        {
            return Task.FromResult(this.GetCatalogNames());
        }

        public Catalog GetCatalog(string name)
        {
            return Data.Catalogs[name];
        }

        public async Task<Catalog> GetCatalogAsync(string name)
        {
            await Task.Delay(5); //do some IO

            return Data.Catalogs[name];
        }

        public ItemDetails GetItemDetails(string code)
        {
            return Data.Details[code];
        }

        public Task<ItemDetails> GetItemDetailsAsync(string code)
        {
            return Task.FromResult(Data.Details[code]);
        }
    }

    static class Data
    {
        const int NumberOfItems = 500;
        static readonly string[] CatalogNames = new string[] { "Books", "Computers", "Tablets", "Consoles", "Games" };

        public static readonly Dictionary<string, Catalog> Catalogs;

        //item code as key
        public static readonly Dictionary<string, ItemDetails> Details;

        static Data()
        {
            var itemsPerCatalog = NumberOfItems / CatalogNames.Length;

            var rnd = new Random(1337);

            var items = Enumerable.Range(0, NumberOfItems)
                                  .Select(i =>
                                      new Item
                                      {
                                          Code = string.Format("i{0}", i),
                                          Description = string.Format("Description for item {0}", i),
                                          Price = rnd.Next(100) + 0.49M
                                      }).ToArray();

            Details = items.Select(i =>
                new ItemDetails
                {
                    Item = i,
                    DateTimeField = new DateTime(rnd.Next(1900, 2014), rnd.Next(1, 13), rnd.Next(1, 29)),
                    GuidField = Guid.NewGuid(),
                    IntField = rnd.Next(),
                    LongField = long.MaxValue - rnd.Next(),
                    NullableDoubleField = rnd.Next() % 2 == 0 ? (double?)rnd.NextDouble() : null,
                    RandomTypeField = Enumerable.Range(0, rnd.Next(5))
                                                .Select(r =>
                                                    new RandomType
                                                    {
                                                        CharField = (char)rnd.Next(256),
                                                        IntArrayField = new int[] { rnd.Next(), rnd.Next(), rnd.Next() }
                                                    }).ToArray()

                }).ToDictionary(d => d.Item.Code, d => d);

            Catalogs = Enumerable.Range(0, CatalogNames.Length)
                                     .Select(c =>
                                         new Catalog
                                         {
                                             Name = CatalogNames[c],
                                             Items = Details.Values.Skip(c * itemsPerCatalog).Take(itemsPerCatalog).Select(d => d.Item).ToArray()
                                         }).ToDictionary(c => c.Name, c => c);
        }

    }
}
