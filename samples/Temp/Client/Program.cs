using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.Http.Client;
//using NetRpcManager = NetRpc.Grpc.NetRpcManager;
using NetRpcManager = NetRpc.Http.Client.NetRpcManager;

namespace Client
{
    class Program
    {
        static IService _p;

        static async Task Main(string[] args)
        {
            _p = NetRpc.RabbitMQ.NetRpcManager.CreateClientProxy<IService>(TestHelper.Helper.GetMQOptions()).Proxy;

            for (int i = 0; i < 50; i++)
            {
                var i1 = i;
                Task.Run(() => { Run(i1); });
            }

            Console.WriteLine("end");
            Console.Read();
        }

        static async Task Run(int index)
        {
            try
            {
                string path;
                if (index % 2 == 0)
                {
                    path = @"d:\7\test\3.rar";
                }
                else
                {
                    path = @"d:\7\test\4.rar";
                }
                var s = File.OpenRead(path);
                await _p.Call3(s, index, d => Console.WriteLine($"index:{index}, prog:{d}"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}