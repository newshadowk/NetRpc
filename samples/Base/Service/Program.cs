using System;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            //RabbitMQ
            var serviceProxy = Helper.OpenRabbitMQService(new Service(), new ServiceAsync());
            //add a Middleware
            serviceProxy.UseMiddleware<TestGlobalExceptionMiddleware>("testArg1");

            //Grpc wrap Exception to FaultException
            Helper.OpenGrpcService(true, new Service(), new ServiceAsync());
            Console.WriteLine("Service Opened.");
            Console.Read();
        }
    }
}