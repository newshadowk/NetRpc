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
                mOpt, new ContractParam<IService, Service>(), new ContractParam<IServiceAsync, ServiceAsync>());
            mqHost.RunAsync();

            //grpc
            var grpcHost = NetRpc.Grpc.NManager.CreateHost(Helper.GetGrpcServiceOptions(),
                null, new ContractParam<IService, Service>(), new ContractParam<IServiceAsync, ServiceAsync>());
            grpcHost.RunAsync();
            Console.Read();
        }
    }
}