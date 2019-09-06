using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetRpc
{
    public static class ExceptionExtensions
    {
        public static void GetObjectData(this Exception e, SerializationInfo info)
        {
            foreach (var p in e.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                info.AddValue(p.Name, p.GetValue(e).ToBytes());
        }

        public static void SetObjectData(this Exception e, SerializationInfo info)
        {
            foreach (var p in e.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var bytes = (byte[]) info.GetValue(p.Name, typeof(byte[]));
                var value = bytes.ToObject();
                p.SetValue(e, value);
            }
        }

        private static byte[] ToBytes(this object obj)
        {
            if (obj == default)
                return default;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Flush();
                return stream.ToArray();
            }
        }

        private static object ToObject(this byte[] bytes)
        {
            if (bytes == default)
                return default;

            using (var stream = new MemoryStream(bytes, 0, bytes.Length, false))
            {
                var formatter = new BinaryFormatter();
                var data = formatter.Deserialize(stream);
                stream.Flush();
                return data;
            }
        }
    }
}