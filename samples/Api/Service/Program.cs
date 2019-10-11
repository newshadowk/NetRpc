using System;
using DataContract;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            //rabbitMq
            var mOpt = new MiddlewareOptions();
            mOpt.UseMiddleware<TestGlobalExceptionMiddleware>();
            var mqHost = NetRpcManager.CreateHost(Helper.GetMQOptions(),
                mOpt, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            mqHost.RunAsync();

            //grpc
            var grpcHost = NetRpc.Grpc.NetRpcManager.CreateHost(Helper.GetGrpcServiceOptions(),
                null, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            grpcHost.RunAsync();
            Console.Read();
        }
    }
}