using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ;

/// <summary>
/// for not .net3.1;
/// .net 3.1 have not implemented yet.
/// </summary>
public sealed class RabbitMQHostedService : IHostedService
{
    private readonly BusyFlag _busyFlag;
    private readonly RequestHandler _requestHandler;
    private Service? _service;
    private readonly ILogger _logger;

    public RabbitMQHostedService(IOptions<RabbitMQServiceOptions> mqOptions, BusyFlag busyFlag, RequestHandler requestHandler, ILoggerFactory factory)
    {
        _busyFlag = busyFlag;
        _logger = factory.CreateLogger("NetRpc");
        _requestHandler = requestHandler;
        Reset(mqOptions.Value);
    }

    private void Reset(MQOptions opt)
    {
        if (_service != null)
        {
            _service.Dispose();
            _service.ReceivedAsync -= ServiceReceivedAsync;
        }

        _service = new Service(opt.CreateConnectionFactory(), opt.RpcQueue, opt.PrefetchCount, opt.MaxPriority, opt.Durable,
            opt.AutoDelete, opt.RetryCount, _logger);
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