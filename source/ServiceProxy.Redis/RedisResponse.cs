using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    /// <summary>
    /// Raw format: HeaderSize + Header + Data
    /// Header format: RequestId
    /// Data: Result
    /// </summary>
    class RedisResponse
    {
        public RedisResponse(string requestId, ResponseData response)
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

            var resultBytes = ArrayExtensions.ToBinary(this.Response);

            var redisResponseBytes = new byte[requestIdSizeBytes.Length + requestIdBytes.Length + resultBytes.Length];

            //Copy header size
            Array.Copy(requestIdSizeBytes, 0, redisResponseBytes, 0, requestIdSizeBytes.Length);

            //Copy header
            Array.Copy(requestIdBytes, 0, redisResponseBytes, requestIdSizeBytes.Length, requestIdBytes.Length);

            //Copy result
            Array.Copy(resultBytes, 0, redisResponseBytes, requestIdSizeBytes.Length + requestIdBytes.Length, resultBytes.Length);

            return redisResponseBytes;

        }

        public static RedisResponse FromBinary(byte[] redisResponseBytes)
        {
            var requestIdSize = BitConverter.ToInt32(redisResponseBytes.Slice(0, 4), 0);
            var requestId = Encoding.UTF8.GetString(redisResponseBytes.Slice(4, requestIdSize));

            var responseBytes = redisResponseBytes.Slice(4 + requestIdSize, redisResponseBytes.Length - (4 + requestIdSize));
            var response = ArrayExtensions.ToObject<ResponseData>(responseBytes);

            return new RedisResponse(requestId, response);
        }
    }
}
