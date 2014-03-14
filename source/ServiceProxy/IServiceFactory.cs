using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy
{
    public interface IServiceFactory
    {
        IService CreateService<TService>() where TService : class;

        IService CreateService(string serviceName);

        IService CreateService(Type serviceType);
    }
}
