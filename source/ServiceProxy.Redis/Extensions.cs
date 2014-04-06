using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Redis
{
    static class ArrayExtensions
    {
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

    static class TaskExtensions
    {
        public static async Task<T> IgnoreException<T>(this Task<T> task, params Type[] exceptionTypes)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                if (exceptionTypes.Any(type => type.IsAssignableFrom(ex.GetType())))
                {
                    return default(T);
                }

                throw;
            }
        }
    }
}
