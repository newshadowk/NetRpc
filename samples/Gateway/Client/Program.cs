using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc.Grpc;
using NetRpc.Http.Client;
using Helper = TestHelper.Helper;
using NetRpcManager = NetRpc.RabbitMQ.NetRpcManager;

namespace Client
{
    class Program
    {
        static IService _proxyAsync;
        static IService2 _proxyAsync2;

        static async Task Main(string[] args)
        {
            Console.WriteLine("---- MQ test ----");
            var mqOptions = Helper.GetMQOptions();
            _proxyAsync = NetRpcManager.CreateClientProxy<IService>(mqOptions).Proxy;
            _proxyAsync2 = NetRpcManager.CreateClientProxy<IService2>(mqOptions).Proxy;
            await TestAsync();

            Console.WriteLine("---- Http test ----");
            var httpOptions = new HttpClientOptions {ApiUrl = "http://localhost:5000", SignalRHubUrl = "http://localhost:5000/callback"};
            _proxyAsync = NetRpc.Http.Client.NetRpcManager.CreateClientProxy<IService>(httpOptions).Proxy;
            _proxyAsync2 = NetRpc.Http.Client.NetRpcManager.CreateClientProxy<IService2>(httpOptions).Proxy;
            await TestAsync();

            Console.WriteLine("---- Grpc test ----");
            GrpcClientOptions grpcOpt = new GrpcClientOptions{Host = "localhost", Port = 50000};
            _proxyAsync = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService>(grpcOpt).Proxy;
            _proxyAsync2 = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService2>(grpcOpt).Proxy;
            await TestAsync();

            Console.WriteLine("---- end ----");
            Console.Read();
        }

        private static async Task TestAsync()
        {
            await Test_Call();
            await Test_Call2();
            await Test_ComplexCallAsync();
        }

        private static async Task Test_Call()
        {
            Console.Write("[Call]...Send 123...");
            await _proxyAsync.Call("123");
            Console.WriteLine("end.");
        }

        private static async Task Test_Call2()
        {
            Console.Write("[Call2]...Send 123...");
            await _proxyAsync2.Call2("123");
            Console.WriteLine("end.");
        }

        private static async Task Test_ComplexCallAsync()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
                var complexStream = await _proxyAsync.ComplexCallAsync(
                    new CustomObj {Date = DateTime.Now, Name = "ComplexCall"},
                    stream,
                    async i => Console.Write(", " + i.Progress),
                    default);

                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
                Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
            }
        }
    }
}