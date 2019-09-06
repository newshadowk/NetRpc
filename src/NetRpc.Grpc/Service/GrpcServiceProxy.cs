using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Base;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    internal sealed class GrpcServiceProxy : IHostedService
    {
        private Service _service;
        private readonly MessageCallImpl _messageCallImpl;

        public GrpcServiceProxy(IOptionsMonitor<GrpcServiceOptions> options, IServiceProvider serviceProvider)
        {
            _messageCallImpl = new MessageCallImpl(serviceProvider);
            _service = new Service(options.CurrentValue.Ports, _messageCallImpl);
            options.OnChange(i =>
            {
                _service?.Dispose();
                _service = new Service(i.Ports, _messageCallImpl);
                _service.Open();
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _service.Open();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("_service.Dispose()");

            _service.Dispose();


            return Task.Run(() =>
            {
                while (_messageCallImpl.IsHanding)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("IsCancellationRequested");
                        break;
                    }

                    Thread.Sleep(1000);
                    Console.WriteLine("IsHanding?");
                }
            }, cancellationToken);
        }
    }
}