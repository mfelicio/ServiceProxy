using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Services.InMemory
{
    public class FooService : IFooService
    {
        private readonly ICatalogService catalogService;

        public FooService(ICatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        public void DoNothing()
        {
            //like nothing
        }
    
        public async Task AsyncOperationThatTakesTimeAndMayFail(int operationTime, bool fail)
        {
            await Task.Delay(operationTime);

            if (fail)
            {
                throw new ApplicationException("You asked for it!");
            }
        }

        public IAsyncResult BeginSum(int arg1, int arg2, AsyncCallback asyncCallback, object asyncState)
        {
            var t = Task.Factory.StartNew((s) => arg1 + arg2, asyncState);
            
            //introducing some delay on purpose and signaling completion when the sum and the delay are done
            Task.Factory.ContinueWhenAll(new Task[] { t, Task.Delay(50)}, ts => asyncCallback(t));

            return t;
        }

        public int EndSum(IAsyncResult asyncResult)
        {
            var t = asyncResult as Task<int>;
            return t.Result;
        }

        public Catalog GetRandomCatalog()
        {
            var random = new Random();
            var names = this.catalogService.GetCatalogNames().ToArray();

            var catalogName = names[random.Next(names.Length)];

            return this.catalogService.GetCatalog(catalogName);
        }
    }
}
