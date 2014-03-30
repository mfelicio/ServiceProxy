using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Services
{
    public interface IFooService
    {
        void DoNothing();
     
        Task AsyncOperationThatTakesTimeAndMayFail(int operationTime, bool fail);

        IAsyncResult BeginSum(int arg1, int arg2, AsyncCallback asyncCallback, object asyncState);
        int EndSum(IAsyncResult asyncResult);

        Catalog GetRandomCatalog();

    }
}
