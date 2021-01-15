using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using NetRpc;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //register
            var services = new ServiceCollection();
            services.AddNGrpcClient(options => options.Url = "http://localhost:50001");
            services.AddNClientContract<IServiceAsync>();
            services.AddLogging();
            var buildServiceProvider = services.BuildServiceProvider();

            //get service
            var service = buildServiceProvider.GetService<IServiceAsync>();
            var clientProxy = buildServiceProvider.GetService<IClientProxy<IServiceAsync>>();

            ////call remote
            //await service.CallAsync("hello world.");
            //await clientProxy.Proxy.CallAsync("hello world.");

            //call remote
            for (int i = 0; i < 100; i++)
            {
                int j = 0;
                Task.Run(async () =>
                {
                    j++;
                    while (true)
                    {
                        try
                        {
                            j++;
                            var r = await service.CallAsync(j.ToString());
                            if (j != int.Parse(r))
                            {
                                Console.WriteLine($"{j} !!!!!!!!!!!!!!!!!");
                                Console.Read();
                                throw new Exception();
                            }
                            Console.WriteLine($"revi:{r}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                        
                    }
                });
            }

            Console.Read();
        }
    }
}