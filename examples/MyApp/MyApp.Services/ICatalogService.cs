using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Services
{
    public interface ICatalogService
    {
        IEnumerable<string> GetCatalogNames();
        Task<IEnumerable<string>> GetCatalogNamesAsync();

        Catalog GetCatalog(string name);
        Task<Catalog> GetCatalogAsync(string name);

        ItemDetails GetItemDetails(string code);
        Task<ItemDetails> GetItemDetailsAsync(string code);
    }
}
