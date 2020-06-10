using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    /// <summary>
    /// for not .net3.1;
    /// .net 3.1 have not implemented yet.
    /// </summary>
    public sealed class RabbitMQServiceProxy : IHostedService
    {
        private readonly BusyFlag _busyFlag;
        private RequestHandler _requestHandler;
        private Service _service;
        private readonly ILogger _logger;

        public RabbitMQServiceProxy(IOptions<RabbitMQServiceOptions> mqOptions, BusyFlag busyFlag, IServiceProvider serviceProvider, ILoggerFactory factory)
        {
            _busyFlag = busyFlag;
            _logger = factory.CreateLogger("NetRpc");
            Reset(mqOptions.Value, serviceProvider);
        }

        private void Reset(MQOptions opt, IServiceProvider serviceProvider)
        {
            if (_service != null)
            {
                _service.Dispose();
                _service.ReceivedAsync -= ServiceReceivedAsync;
            }

            _service = new Service(opt.CreateConnectionFactory(), opt.RpcQueue, opt.PrefetchCount, _logger);
            _requestHandler = new RequestHandler(serviceProvider, ChannelType.RabbitMQ);
            _service.ReceivedAsync += ServiceReceivedAsync;
        }

        private async Task ServiceReceivedAsync(object sender, global::RabbitMQ.Base.EventArgsT<CallSession> e)
        {
            _busyFlag.Increment();
            try
            {
#if NETSTANDARD2_1 || NETCOREAPP3_1
                await
#endif 

                using var connection = new RabbitMQServiceConnection(e.Value);
                await _requestHandler.HandleAsync(connection);
            }
            finally
            {
                _busyFlag.Decrement();
            }
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