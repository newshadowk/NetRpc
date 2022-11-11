using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Proxy.RabbitMQ;

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
            var bytes = (byte[])info.GetValue(p.Name, typeof(byte[]))!;
            var value = bytes.ToObject();
            p.SetValue(e, value);
        }
    }

    private static byte[]? ToBytes(this object? obj)
    {
        if (obj == null)
            return null;

        using var stream = new MemoryStream();
        var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
        formatter.Serialize(stream, obj);
#pragma warning restore SYSLIB0011
        stream.Flush();
        return stream.ToArray();
    }

    private static object? ToObject(this byte[]? bytes)
    {
        if (bytes == null)
            return null;

        using var stream = new MemoryStream(bytes, 0, bytes.Length, false);
        var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
        var data = formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011
        stream.Flush();
        return data;
    }
}