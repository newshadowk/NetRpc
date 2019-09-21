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
                await _p.Call3("12");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
      

            Console.WriteLine("end");
            Console.Read();
        }
    }
}