using Castle.Zmq;
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
        public static Guid NewIdentity(this IZmqContext context)
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

        public static IZmqSocket CreateNonBlockingReadonlySocket(this IZmqContext context, SocketType socketType, TimeSpan receiveTimeout)
        {
            var socket = CreateReadonlySocket(context, socketType);

            socket.SetOption(SocketOpt.RCVTIMEO, (int)receiveTimeout.TotalMilliseconds);

            return socket;
        }

        public static IZmqSocket CreateReadonlySocket(this IZmqContext context, SocketType socketType)
        {
            var socket = context.CreateSocket(socketType);

            socket.SetOption(SocketOpt.RCVHWM, 0);
            socket.SetOption(SocketOpt.LINGER, 0);

            return socket;
        }

        public static IZmqSocket CreateWriteonlySocket(this IZmqContext context, SocketType socketType)
        {
            var socket = context.CreateSocket(socketType);

            socket.SetOption(SocketOpt.SNDHWM, 0);
            socket.SetOption(SocketOpt.LINGER, 0);

            return socket;
        }

        public static IZmqSocket CreateNonBlockingSocket(this IZmqContext context, SocketType socketType, TimeSpan timeout)
        {
            var socket = context.CreateSocket(socketType);

            socket.SetOption(SocketOpt.RCVTIMEO, (int)timeout.TotalMilliseconds);

            socket.SetOption(SocketOpt.RCVHWM, 0);
            socket.SetOption(SocketOpt.SNDHWM, 0);

            socket.SetOption(SocketOpt.LINGER, 0);

            return socket;
        }
    }
}
