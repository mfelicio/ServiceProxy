using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy
{
    [Serializable]
    public class ResponseData
    {
        public ResponseData(object data)
        {
            this.Data = data;
            this.Exception = null;
        }

        public ResponseData(Exception error)
        {
            this.Data = null;
            this.Exception = error;
        }

        public object Data { get; private set; }
        public Exception Exception { get; private set; }
    }
}
