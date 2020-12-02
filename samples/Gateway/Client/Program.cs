using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.Http.Client;
using Helper = TestHelper.Helper;
using NManager = NetRpc.RabbitMQ.NManager;

namespace Client
{
    internal class Program
    {
        private static ClientProxy<IService> _c1;
        private static ClientProxy<IService2> _c2;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("---- MQ test ----");
            var mqOptions = Helper.GetMQOptions();
            _c1 = NManager.CreateClientProxy<IService>(mqOptions);
            _c2 = NManager.CreateClientProxy<IService2>(mqOptions);
            await TestAsync();

            Console.WriteLine("---- Http test ----");
            var httpOptions = new HttpClientOptions {ApiUrl = "http://localhost:5000", SignalRHubUrl = "http://localhost:5000/callback"};
            _c1 = NetRpc.Http.Client.NManager.CreateClientProxy<IService>(httpOptions);
            _c2 = NetRpc.Http.Client.NManager.CreateClientProxy<IService2>(httpOptions);
            await TestAsync();

            Console.WriteLine("---- Grpc test ----");
            var grpcOpt = new GrpcClientOptions {Url = "http://localhost:50000"};
            _c1 = NetRpc.Grpc.NManager.CreateClientProxy<IService>(grpcOpt);
            _c2 = NetRpc.Grpc.NManager.CreateClientProxy<IService2>(grpcOpt);
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
            _c1.AdditionHeader.Add("k1", "k1 value");
            await _c1.Proxy.Call("123");
            Console.WriteLine("end.");
        }

        private static async Task Test_Call2()
        {
            Console.Write("[Call2]...Send 123...");
            await _c2.Proxy.Call2("123");
            Console.WriteLine("end.");
        }

        private static async Task Test_ComplexCallAsync()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
                var complexStream = await _c1.Proxy.ComplexCallAsync(
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