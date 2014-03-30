using MyApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MyApp.WebAPI.Controllers
{
    public class FooController : ApiController
    {
        private readonly IFooService fooService;

        public FooController(IFooService fooService)
        {
            this.fooService = fooService;
        }

        /// <summary>
        /// Invokes fooService.DoNothing
        /// </summary>
        public void DoNothing()
        {
            this.fooService.DoNothing();
        }

        /// <summary>
        /// Invokes fooService.AsyncOperationThatTakesTimeAndMayFail
        /// </summary>
        /// <param name="operationTime">The time the operation will take to complete</param>
        /// <param name="fail">If true, the operation will complete with an exception</param>
        /// <returns></returns>
        public async Task AsyncOperationThatTakesTimeAndMayFail(int operationTime, bool fail)
        {
            await this.fooService.AsyncOperationThatTakesTimeAndMayFail(operationTime, fail);
        }

        /// <summary>
        /// Invokes fooService.BeginSum/EndSum to get the sum of two arguments
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public async Task<int> Sum(int arg1, int arg2)
        {
            var sumResult = await Task.Factory.FromAsync<int, int, int>(this.fooService.BeginSum, this.fooService.EndSum, arg1, arg2, null);
            return sumResult;
        }

        /// <summary>
        /// Invokes fooService.GetRandomCatalog to get a random catalog
        /// </summary>
        /// <returns></returns>
        public Catalog GetRandomCatalog()
        {
            return this.fooService.GetRandomCatalog();
        }
    }
}
