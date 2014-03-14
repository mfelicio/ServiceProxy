using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Tests.Stubs
{
    public class DependencyResolver : IDependencyResolver
    {
        public object Resolve(Type type)
        {
            if (type == typeof(ITestService) || type == typeof(ITestService2))
            {
                return new TestService();
            }

            return Activator.CreateInstance(type);
        }
    }
}
