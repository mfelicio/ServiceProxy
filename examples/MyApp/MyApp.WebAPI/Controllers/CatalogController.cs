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
    public class CatalogController : ApiController
    {
        private readonly ICatalogService catalogService;

        public CatalogController(ICatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        public IEnumerable<string> GetCatalogNames()
        {
            return this.catalogService.GetCatalogNames();
        }

        public Catalog GetCatalogs(string name)
        {
            return this.catalogService.GetCatalog(name);
        }

        public ItemDetails GetDetails(string code)
        {
            return this.catalogService.GetItemDetails(code);
        }

    }
}