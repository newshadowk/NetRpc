using System;
using DataContract;
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
                null, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            mqHost.StartAsync();

            //grpc
            var grpcHost = NetRpc.Grpc.NetRpcManager.CreateHost(Helper.GetGrpcServiceOptions(),
                null, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            grpcHost.StartAsync();
            Console.Read();
        }
    }
}