using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq
{
    /// <summary>
    /// Raw format: HeaderSize+Header+Data
    /// Header format: Id:Service:Operation
    /// Data format: byte[](Arguments)
    /// </summary>
    class ZmqRequest
    {
        public ZmqRequest(string id, RequestData request)
        {
            this.Id = id;
            this.Request = request;
        }

        public string Id { get; private set; }

        public RequestData Request { get; private set; }

        public byte[] ToBinary()
        {
            var header = string.Format("{0}:{1}:{2}", this.Id, this.Request.Service, this.Request.Operation);

            var headerBytes = Encoding.UTF8.GetBytes(header);
            var headerSizeBytes = BitConverter.GetBytes(headerBytes.Length);

            var argumentsBytes = ArrayExtensions.ToBinary(this.Request.Arguments);

            var zmqRequestBytes = new byte[4 + headerBytes.Length + argumentsBytes.Length];

            //copy header size
            Array.Copy(headerSizeBytes, 0, zmqRequestBytes, 0, headerSizeBytes.Length);

            //copy header
            Array.Copy(headerBytes, 0, zmqRequestBytes, headerSizeBytes.Length, headerBytes.Length);

            //copy arguments
            Array.Copy(argumentsBytes, 0, zmqRequestBytes, headerSizeBytes.Length + headerBytes.Length, argumentsBytes.Length);

            return zmqRequestBytes;
        }

        public static ZmqRequest FromBinary(byte[] zmqRequestBytes)
        {
            var headerSize = BitConverter.ToInt32(zmqRequestBytes.Slice(0, 4), 0);
            var headerBytes = zmqRequestBytes.Slice(4, headerSize);
            var argumentsBytes = zmqRequestBytes.Slice(4 + headerSize, zmqRequestBytes.Length - (4 + headerSize));

            var header = Encoding.UTF8.GetString(headerBytes);
            var headerValues = header.Split(':');

            var arguments = ArrayExtensions.ToObject<object[]>(argumentsBytes);

            var request = new RequestData(headerValues[1], headerValues[2], arguments);

            return new ZmqRequest(headerValues[0], request);
        }

    }
}
