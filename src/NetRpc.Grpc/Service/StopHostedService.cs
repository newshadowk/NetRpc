using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NetRpc.Grpc
{
    /// <summary>
    /// for .net 3.1
    /// </summary>
    internal sealed class StopHostedService : IHostedService
    {
        private readonly BusyFlag _busyFlag;

        public StopHostedService(BusyFlag busyFlag)
        {
            _busyFlag = busyFlag;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            while (_busyFlag.IsHandling)
            {
                await Task.Delay(100);
            }
        }
    }
}