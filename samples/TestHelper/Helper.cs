using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NetRpc.RabbitMQ;

namespace TestHelper
{
    public static class Helper
    {
        public static MQOptions GetMQOptions()
        {
            //config your RabbitMQ parameters before run
            var user = "testuser";
            var password = "1";
            var host = "m.k8s.yx.com";
            //var host = "localhost";
            var virtualHost = "testvh";
            var port = 35672;
            var rpcQueue = "rpc_test";
            var prefetchCount = 1;
            var p = new MQOptions(host, virtualHost, rpcQueue, port, user, password, prefetchCount, 5, true, false);
            return p;
        }

        public static string GetTestFilePath()
        {
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
}