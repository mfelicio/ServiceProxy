using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy
{
    [Serializable]
    public class RequestData
    {
        public RequestData(string service, string operation, object[] arguments)
        {
            this.Service = service;
            this.Operation = operation;
            this.Arguments = arguments;
        }

        public string Service { get; private set; }
        public string Operation { get; private set; }
        public object[] Arguments { get; private set; }
    }
}
