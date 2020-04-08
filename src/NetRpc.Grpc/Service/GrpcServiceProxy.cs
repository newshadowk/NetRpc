using System;
using System.Threading;
using System.Threading.Tasks;
using Proxy.Grpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    internal sealed class GrpcServiceProxy : IHostedService
    {
        private Service _service;
        private readonly MessageCallImpl _messageCallImpl;
        private readonly IDisposable _optionDisposable;

        public GrpcServiceProxy(IOptionsMonitor<GrpcServiceOptions> options, IServiceProvider serviceProvider, ILoggerFactory factory)
        {
            _messageCallImpl = new MessageCallImpl(serviceProvider, factory);
            _service = new Service(options.CurrentValue.Ports, _messageCallImpl);
            _optionDisposable = options.OnChange(i =>
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
            _optionDisposable.Dispose();
            _service.Dispose();
            return Task.Run(() =>
            {
                while (_messageCallImpl.IsHanding)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    Thread.Sleep(1000);
                }
            }, cancellationToken);
        }
    }
}