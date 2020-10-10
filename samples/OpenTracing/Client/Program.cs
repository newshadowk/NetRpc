using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NetRpc.RabbitMQ.NManager.CreateClientProxy<IService>(TestHelper.Helper.GetMQOptions());
            await p.Proxy.Call("msg");

            //var pp = NetRpc.Grpc.NManager.CreateClientProxy<IService_1>(new GrpcClientOptions {Url = "http://localhost:50002"});
            //var ret =  await pp.Proxy.Call_1(new SendObj(), 1, false, null, default);

            Console.WriteLine("--- end ---");
            Console.Read();
        }
    }
}