using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ;

public sealed class RabbitMQHostedService : IHostedService
{
    private readonly BusyFlag _busyFlag;
    private readonly RequestHandler _requestHandler;
    private readonly Service? _service;

    public RabbitMQHostedService(IOptions<RabbitMQServiceOptions> opt, BusyFlag busyFlag, RequestHandler requestHandler, ILoggerFactory factory)
    {
        _busyFlag = busyFlag;
        var logger = factory.CreateLogger("NetRpc");
        _requestHandler = requestHandler;

        _service = new Service(opt.Value.CreateConnectionFactory(), opt.Value.RpcQueue, opt.Value.PrefetchCount, opt.Value.MaxPriority, logger);
        _service.ReceivedAsync += ServiceReceivedAsync;
    }

    private async Task ServiceReceivedAsync(object sender, global::RabbitMQ.Base.EventArgsT<CallSession> e)
    {
        _busyFlag.Increment();
        try
        {
            await using var connection = new RabbitMQServiceConnection(e.Value);
            await _requestHandler.HandleAsync(connection, ChannelType.RabbitMQ);
        }
        finally
        {
            _busyFlag.Decrement();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _service?.Open();
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

        _service?.Dispose();
    }
}