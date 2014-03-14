using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq
{
    /// <summary>
    /// Raw format: HeaderSize + Header + Data
    /// Header format: RequestId
    /// Data: Result
    /// </summary>
    class ZmqResponse
    {
        public ZmqResponse(string requestId, ResponseData response)
        {
            this.RequestId = requestId;
            this.Response = response;
        }

        public string RequestId { get; private set; }
        public ResponseData Response { get; private set; }

        public byte[] ToBinary()
        {
            var requestIdBytes = Encoding.UTF8.GetBytes(this.RequestId);
            var requestIdSizeBytes = BitConverter.GetBytes(requestIdBytes.Length);

            var responseBytes = ArrayExtensions.ToBinary(this.Response);

            var zmqResponseBytes = new byte[requestIdSizeBytes.Length + requestIdBytes.Length + responseBytes.Length];

            //Copy header size
            Array.Copy(requestIdSizeBytes, 0, zmqResponseBytes, 0, requestIdSizeBytes.Length);

            //Copy header
            Array.Copy(requestIdBytes, 0, zmqResponseBytes, requestIdSizeBytes.Length, requestIdBytes.Length);

            //Copy result
            Array.Copy(responseBytes, 0, zmqResponseBytes, requestIdSizeBytes.Length + requestIdBytes.Length, responseBytes.Length);

            return zmqResponseBytes;

        }

        public static ZmqResponse FromBinary(byte[] zmqResponseBytes)
        {
            var requestIdSize = BitConverter.ToInt32(zmqResponseBytes.Slice(0, 4), 0);
            var requestId = Encoding.UTF8.GetString(zmqResponseBytes.Slice(4, requestIdSize));

            var responseBytes = zmqResponseBytes.Slice(4 + requestIdSize, zmqResponseBytes.Length - (4 + requestIdSize));
            var response = ArrayExtensions.ToObject<ResponseData>(responseBytes);

            return new ZmqResponse(requestId, response);
        }
    }
}
