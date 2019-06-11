using System.IO;
using System.Linq;
using NetRpc.RabbitMQ;
using System.Reflection;
using System.Text;
using Grpc.Core;

namespace TestHelper
{
    public static class Helper
    {
        public static ServiceProxy OpenRabbitMQService(params object[] instances)
        {
            var service = NetRpcManager.CreateServiceProxy(GetMQParam(), instances);
            service.Open();
            return service;
        }

        public static NetRpc.Grpc.ServiceProxy OpenGrpcService(params object[] instances)
        {
            var service = NetRpc.Grpc.NetRpcManager.CreateServiceProxy(new ServerPort("0.0.0.0", 50001, ServerCredentials.Insecure), instances);
            service.Open();
            return service;
        }

        public static MQParam GetMQParam()
        {
            //config your RabbitMQ parameters before run
            string user = "testuser";
            string password = "1";
            string host = "test7";
            string virtualHost = "testvh";
            int port = 5672;
            string rpcQueue = "rpc_test";
            int prefetchCount = 2;
            var p = new MQParam(host, virtualHost, rpcQueue, port, user, password, prefetchCount);
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
            byte[] bs = new byte[size];
            var readCount = stream.Read(bs, 0, size);
            var list = bs.ToList();
            list.RemoveRange(readCount, list.Count - readCount);
            var tgtBs = list.ToArray();
            var s = Encoding.UTF8.GetString(tgtBs);
            return s;
        }
    }
}