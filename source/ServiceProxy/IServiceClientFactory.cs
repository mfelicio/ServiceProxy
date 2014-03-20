using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy
{
    public interface IServiceClientFactory
    {
        TService CreateServiceClient<TService>(int? timeout = null) where TService : class;
        object CreateServiceClient(Type serviceType, int? timeout = null);
    }
}
