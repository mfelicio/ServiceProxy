using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy
{
    public interface IServiceClientFactory
    {
        TService CreateServiceClient<TService>() where TService : class;
        object CreateServiceClient(Type serviceType);
    }
}
