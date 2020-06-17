using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Proxy.Grpc;

namespace NetRpc.Grpc
{
    /// <summary>
    /// for not .net 3.1
    /// </summary>
    internal sealed class GrpcServiceProxy : IHostedService
    {
        private readonly Service _service;
        private readonly BusyFlag _busyFlag;

        public GrpcServiceProxy(IOptions<NGrpcServiceOptions> options, MessageCallImpl messageCall, BusyFlag busyFlag)
        {
            _busyFlag = busyFlag;
            _service = new Service(options.Value.Ports, messageCall);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _service.Open();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            while (_busyFlag.IsHandling)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    await Task.Delay(10, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _service.Dispose();
        }
    }
}