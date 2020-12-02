using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract1;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using Helper = TestHelper.Helper;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await RunGrpcAsync();
        }

        private static async Task RunGrpcAsync()
        {
            var host = NManager.CreateHost(50002, null, new ContractParam(typeof(IService1), typeof(Service1)));
            await host.RunAsync();
        }
    }

    internal class Service1 : IService1
    {
        public async Task<Ret> Call(InParam p, int i, Stream stream, Func<int, Task> progs, CancellationToken token)
        {
            Console.WriteLine($"{p}, {i}, {Helper.ReadStr(stream)}");

            for (var i1 = 0; i1 < 3; i1++)
            {
                await progs(i1);
                await Task.Delay(100, token);
            }

            return new Ret
            {
                Stream = File.OpenRead(Helper.GetTestFilePath()),
                P1 = "return p1"
            };
        }
    }
}