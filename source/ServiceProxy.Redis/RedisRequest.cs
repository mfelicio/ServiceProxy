using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    /// <summary>
    /// Raw format: HeaderSize+Header+Data
    /// Header format: ReceiveQueue:Id:Service:Operation
    /// Data format: byte[](Arguments)
    /// </summary>
    class RedisRequest
    {
        public RedisRequest(string receiveQueue, string id, RequestData request)
        {
            this.ReceiveQueue = receiveQueue;
            this.Id = id;
            this.Request = request;
        }

        public string ReceiveQueue { get; private set; }
        public string Id { get; private set; }

        public RequestData Request { get; private set; }

        public byte[] ToBinary()
        {
            var header = string.Format("{0}:{1}:{2}:{3}", this.ReceiveQueue, this.Id, this.Request.Service, this.Request.Operation);

            var headerBytes = Encoding.UTF8.GetBytes(header);
            var headerSizeBytes = BitConverter.GetBytes(headerBytes.Length);

            var argumentsBytes = ArrayExtensions.ToBinary(this.Request.Arguments);

            var redisRequestBytes = new byte[4 + headerBytes.Length + argumentsBytes.Length];

            //copy header size
            Array.Copy(headerSizeBytes, 0, redisRequestBytes, 0, headerSizeBytes.Length);

            //copy header
            Array.Copy(headerBytes, 0, redisRequestBytes, headerSizeBytes.Length, headerBytes.Length);

            //copy arguments
            Array.Copy(argumentsBytes, 0, redisRequestBytes, headerSizeBytes.Length + headerBytes.Length, argumentsBytes.Length);

            return redisRequestBytes;
        }

        public static RedisRequest FromBinary(byte[] redisRequestBytes)
        {
            var headerSize = BitConverter.ToInt32(redisRequestBytes.Slice(0, 4), 0);
            var headerBytes = redisRequestBytes.Slice(4, headerSize);
            var argumentsBytes = redisRequestBytes.Slice(4 + headerSize, redisRequestBytes.Length - (4 + headerSize));

            var header = Encoding.UTF8.GetString(headerBytes);
            var headerValues = header.Split(':');

            var arguments = ArrayExtensions.ToObject<object[]>(argumentsBytes);

            var request = new RequestData(headerValues[2], headerValues[3], arguments);

            return new RedisRequest(headerValues[0], headerValues[1], request);
        }

    }
}
