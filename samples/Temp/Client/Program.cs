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
            try
            {
                await _p.Call();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            //_p = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure)).Proxy;

            //for (int i = 0; i < 100; i++)
            //{
            //    var i1 = i;
            //    Task.Run(() => { Run(i1); });
            //}

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
                    //path = @"d:\7\test\3.rar";
                    path = @"d:\7\test\1.zip";
                }
                else
                {
                    //path = @"d:\7\test\4.rar";
                    path = @"d:\7\test\1.zip";
                }
                var s = File.OpenRead(path);
                await _p.Call3(s, index, d => Console.WriteLine($"index:{index}, prog:{d}"));
                Console.WriteLine($"index:{index}, end-----------------------------");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}