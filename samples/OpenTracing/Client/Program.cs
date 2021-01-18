using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.Grpc;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //var p = NManager.CreateClientProxy<IService>(Helper.GetMQOptions());
            //await p.Proxy.Call("msg");

            var pp = NManager.CreateClientProxy<IService>(new GrpcClientOptions {Url = "http://localhost:50001"});
            //var ret = await pp.Proxy.Call_1(new SendObj(), 1, false, null, default);
            await pp.Proxy.Call("msg");

            Console.WriteLine("--- end ---");
            Console.Read();
        }
    }
}