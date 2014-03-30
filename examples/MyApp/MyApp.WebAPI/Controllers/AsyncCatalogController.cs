using MyApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MyApp.WebAPI.Controllers
{
    public class AsyncCatalogController : ApiController
    {
        private readonly ICatalogService catalogService;

        public AsyncCatalogController(ICatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        public async Task<IEnumerable<string>> GetCatalogNamesAsync()
        {
            return await this.catalogService.GetCatalogNamesAsync();
        }

        public async Task<Catalog> GetCatalogAsync(string name)
        {
            return await this.catalogService.GetCatalogAsync(name);
        }

        public async Task<ItemDetails> GetDetailsAsync(string code)
        {
            return await this.catalogService.GetItemDetailsAsync(code);
        }

    }
}