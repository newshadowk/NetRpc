using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetRpc.Http;

internal class BinaryCacheSerializer
{        
    public object Deserialize(string base64, Type type)
    {
        var binaryFormatter = new BinaryFormatter();
        using var ms = new MemoryStream(Convert.FromBase64String(base64));
        var obj = binaryFormatter.Deserialize(ms);
        return Convert.ChangeType(obj, type);
    }

    public string Serialize(object item)
    {
        var binaryFormatter = new BinaryFormatter();
        using var ms = new MemoryStream();
        binaryFormatter.Serialize(ms, item);
        var data = ms.ToArray();
        return Convert.ToBase64String(data);
    }
}