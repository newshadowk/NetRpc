using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc.Grpc;
using NetRpc.Http.Client;
//using NetRpcManager = NetRpc.Grpc.NetRpcManager;

using NetRpcManager = NetRpc.Http.Client.NetRpcManager;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var openRead = File.OpenRead(@"C:\TempF\17\src.pdf");
            var openReadLength = openRead.Length;


            //var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure));
            var p = NetRpcManager.CreateClientProxy<IService>(new HttpClientOptions() { ApiUrl = "http://localhost:5000", SignalRHubUrl = "http://localhost:5000/callback" });

            using (var fileStream = File.OpenRead(@"d:\7\4.rar"))
            {
                //await p.Proxy.Call(fileStream, Console.WriteLine);
                //await p.Proxy.Call2(fileStream);

                var stream = await p.Proxy.Call3(fileStream, Console.WriteLine);
                const int size = 81920;
                var bs = new byte[size];
                var readCount = stream.Read(bs, 0, size);
                while (readCount > 0)
                {
                    readCount = stream.Read(bs, 0, size);
                    Console.WriteLine(readCount);
                }
                stream.Dispose();
                //Console.WriteLine("end");
            }

            Console.WriteLine("end");
            Console.Read();
        }
    }
}