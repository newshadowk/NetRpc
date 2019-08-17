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

        public GrpcServiceProxy(IOptionsMonitor<GrpcServiceOptions> options, IServiceProvider serviceProvider)
        {
            var messageCallImpl = new MessageCallImpl(serviceProvider);
            _service = new Service(options.CurrentValue.Ports, messageCallImpl);
            options.OnChange(i =>
            {
                _service?.Dispose();
                _service = new Service(i.Ports, messageCallImpl);
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
            _service.Dispose();
            return Task.CompletedTask;
        }
    }
}