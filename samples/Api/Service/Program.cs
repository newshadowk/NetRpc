using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //rabbitMQ
            var mOpt = new MiddlewareOptions();
            mOpt.UseMiddleware<TestGlobalExceptionMiddleware>();
            var mqHost = NManager.CreateHost(Helper.GetMQOptions(),
                mOpt, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            mqHost.RunAsync();

            //grpc
            var grpcHost = NetRpc.Grpc.NManager.CreateHost(Helper.GetGrpcServiceOptions(),
                null, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            grpcHost.RunAsync();
            Console.Read();
        }
    }
}