using System;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProxy = Helper.OpenRabbitMQService(new Service(), new ServiceAsync());
            //add a Middleware
            serviceProxy.UseMiddleware<TestGlobalExceptionMiddleware>("testArg1");

            Helper.OpenGrpcService(new Service(), new ServiceAsync());
            Console.WriteLine("Service Opened.");
            Console.Read();
        }
    }
}