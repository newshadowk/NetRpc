using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Proxy.RabbitMQ;

namespace TestHelper;

public static class Helper
{
    public static MQOptions GetMQOptions()
    {
        return new MQOptions
        {
            Url = "amqp://guest:guest@192.168.0.50:5672/testvh",
            RpcQueue = "rpc.test"
        };
    }

    public static string GetTestFilePath()
    {
        //return @"D:\TestFile\termSrc.pdf";
        var assemblyPath = Assembly.GetExecutingAssembly().CodeBase;
        assemblyPath = assemblyPath.Substring(8, assemblyPath.Length - 8);
        assemblyPath = Path.GetDirectoryName(assemblyPath);
        return Path.Combine(assemblyPath, "TestFile.txt");
    }

    public static string ReadStr(Stream stream)
    {
        const int size = 81920;
        var bs = new byte[size];
        var readCount = stream.ReadAsync(bs, 0, size).Result;
        var list = bs.ToList();
        list.RemoveRange(readCount, list.Count - readCount);
        var tgtBs = list.ToArray();
        var s = Encoding.UTF8.GetString(tgtBs);
        return s;
    }
}