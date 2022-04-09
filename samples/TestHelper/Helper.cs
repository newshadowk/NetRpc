using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NetRpc.RabbitMQ;
using Proxy.RabbitMQ;

namespace TestHelper;

public static class Helper
{
    public static MQOptions GetMQOptions()
    {
        //config your RabbitMQ parameters before run
        var user = "guest";
        var password = "guest";
        var host = "192.168.0.50";
        var virtualHost = "testvh";
        var port = 5672;
        var rpcQueue = "rpc_test";
        var prefetchCount = 1;
        var p = new MQOptions(host, virtualHost, rpcQueue, port, user, password, prefetchCount);
        return p;
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