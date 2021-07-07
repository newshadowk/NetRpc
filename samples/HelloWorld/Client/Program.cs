using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Timer = System.Timers.Timer;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //register
            var services = new ServiceCollection();

            services.AddNGrpcClient(options =>
            {
                options.Url = "http://localhost:50001";
                //options.HeaderHost = "www.baidu.com:8080";
            });
            services.AddNClientContract<IServiceAsync>();
            var sp = services.BuildServiceProvider();

            //get service
            var service = sp.GetService<IServiceAsync>();
            Console.WriteLine("call: hello world.");
            var ret = await service.CallAsync("hello world.");
            Console.WriteLine($"ret: {ret}");

            Console.Read();
        }
    }

    internal class LifetimeEventsHostedService : IHostedService
    {
        private readonly IOptionsMonitor<COptions> _options;

        private readonly Timer _t = new Timer(3000);

        public LifetimeEventsHostedService(IOptionsMonitor<COptions> options)
        {
            options.OnChange(i =>
            {
                Console.WriteLine($"change {i.K1}");
            });
            
            _options = options;
            _t.Elapsed += _t_Elapsed;
            _t.Start();
        }

        private void _t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine($"{_options.CurrentValue.K1}");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    internal class COptions
    {
        public int K1 { get; set; }
    }
}