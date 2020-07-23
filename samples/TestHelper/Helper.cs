using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;

namespace TestHelper
{
    public static class Helper
    {
        public static NGrpcServiceOptions GetGrpcServiceOptions()
        {
            var opt = new NGrpcServiceOptions();
            opt.AddPort("0.0.0.0", 50001);
            return opt;
        }

        public static MQOptions GetMQOptions()
        {
            //config your RabbitMQ parameters before run
            var user = "testuser";
            var password = "1";
            var host = "192.168.1.158";
            var virtualHost = "testvh";
            var port = 5672;
            var rpcQueue = "rpc_test";
            var prefetchCount = 2;
            var p = new MQOptions(host, virtualHost, rpcQueue, port, user, password, prefetchCount);
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