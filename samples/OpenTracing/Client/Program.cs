using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var p = NManager.CreateClientProxy<IService>(Helper.GetMQOptions());
            await p.Proxy.Call("msg");

            //var pp = NetRpc.Grpc.NManager.CreateClientProxy<IService_1>(new GrpcClientOptions {Url = "http://localhost:50002"});
            //var ret =  await pp.Proxy.Call_1(new SendObj(), 1, false, null, default);

            Console.WriteLine("--- end ---");
            Console.Read();
        }
    }
}