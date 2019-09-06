using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using NetRpc.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var o = new GrpcServiceOptions();
            o.AddPort("0.0.0.0", 50001);
            var options = new MiddlewareOptions();
            options.UseCallbackThrottling(1000);
            var host = NetRpcManager.CreateHost(o, options, new Contract<IService, Service>());
            await host.StartAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(Action<int> cb)
        {
            for (int i = 0; i <= 20; i++)
            {
                await Task.Delay(100);
                cb.Invoke(i);
                Console.WriteLine($"Send callback: {i}");
            }
        }
    }
}