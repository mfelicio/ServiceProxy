using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Zmq
{
    static class ArrayExtensions
    {
        public static T[] Slice<T>(this T[] data, int length)
        {
            return data.Slice(0, length);
        }

        public static T[] Slice<T>(this T[] data, int index, int length)
        {
            T[] slice = new T[length];
            Array.Copy(data, index, slice, 0, length);
            return slice;
        }

        public static byte[] ToBinary<T>(T obj)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                return stream.GetBuffer();
            }

        }

        public static T ToObject<T>(byte[] objBytes)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(objBytes))
            {
                return (T)formatter.Deserialize(stream);
            }
        }
    }

    static class ZmqContextExtensions
    {
        public static Guid NewIdentity(this ZeroMQ.ZmqContext context)
        {
            Guid identity;
            while (true)
            {
                identity = Guid.NewGuid();
                if (identity.ToByteArray()[0] != 0)
                {
                    return identity;
                }
            }
        }

        public static ZeroMQ.ZmqSocket CreateNonBlockingReadonlySocket(this ZeroMQ.ZmqContext context, ZeroMQ.SocketType socketType, TimeSpan receiveTimeout)
        {
            var socket = CreateReadonlySocket(context, socketType);

            socket.ReceiveTimeout = receiveTimeout;

            return socket;
        }

        public static ZeroMQ.ZmqSocket CreateReadonlySocket(this ZeroMQ.ZmqContext context, ZeroMQ.SocketType socketType)
        {
            var socket = context.CreateSocket(socketType);

            socket.ReceiveHighWatermark = 0;
            socket.Linger = TimeSpan.FromMilliseconds(0);

            return socket;
        }

        public static ZeroMQ.ZmqSocket CreateWriteonlySocket(this ZeroMQ.ZmqContext context, ZeroMQ.SocketType socketType)
        {
            var socket = context.CreateSocket(socketType);

            socket.SendHighWatermark = 0;
            socket.Linger = TimeSpan.FromMilliseconds(0);

            return socket;
        }

        public static ZeroMQ.ZmqSocket CreateNonBlockingSocket(this ZeroMQ.ZmqContext context, ZeroMQ.SocketType socketType, TimeSpan timeout)
        {
            var socket = context.CreateSocket(socketType);

            socket.ReceiveTimeout = timeout;

            socket.ReceiveHighWatermark = 0;
            socket.SendHighWatermark = 0;

            socket.Linger = TimeSpan.FromMilliseconds(0);

            return socket;
        }
    }
}
