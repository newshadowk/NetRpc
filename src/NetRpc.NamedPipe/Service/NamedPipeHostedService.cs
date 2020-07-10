using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NetRpc.NamedPipe
{
    internal sealed class NamedPipeHostedService : IHostedService
    {
        private readonly NamedPipeServiceGroup _service;
        private readonly BusyFlag _busyFlag;

        public NamedPipeHostedService(NamedPipeServiceGroup group,  BusyFlag busyFlag)
        {
            _busyFlag = busyFlag;
            _service = group;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _service.WaitForConnectionAsync();
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

#if NETSTANDARD2_1
            _service.DisposeAsync();
#else
            _service.Dispose();
#endif
        }
    }
}