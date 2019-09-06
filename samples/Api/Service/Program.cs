using System;
using DataContract;
using NetRpc;
using NetRpc.Grpc;
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
            var mqHost = NetRpc.RabbitMQ.NetRpcManager.CreateHost(Helper.GetMQOptions(),
                null, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            mqHost.StartAsync();

            //grpc
            var grpcHost = NetRpcManager.CreateHost(Helper.GetGrpcServiceOptions(),
                null, new Contract<IService, Service>(), new Contract<IServiceAsync, ServiceAsync>());
            grpcHost.StartAsync();
            Console.Read();
        }
    }
}