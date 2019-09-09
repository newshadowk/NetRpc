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
            var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure));
            using (var fileStream = File.OpenRead(@"d:\7\3.rar"))
            {
                //await p.Proxy.Call(fileStream, Console.WriteLine);
                await p.Proxy.Call2(fileStream);
            }

            Console.WriteLine("end");
            Console.Read();
        }
    }
}