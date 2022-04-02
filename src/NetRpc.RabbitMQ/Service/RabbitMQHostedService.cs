using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;
using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public sealed class RabbitMQHostedService : IHostedService
{
    private readonly BusyFlag _busyFlag;
    private readonly RequestHandler _requestHandler;
    private readonly Service? _service;
    private readonly ILogger _logger;

    public RabbitMQHostedService(IOptions<RabbitMQServiceOptions> opt, BusyFlag busyFlag, RequestHandler requestHandler, ILoggerFactory factory)
    {
        _busyFlag = busyFlag;
        _logger = factory.CreateLogger("NetRpc");
        _requestHandler = requestHandler;

        _service = new Service(opt.Value.CreateConnectionFactory_Recovery_Disabled(), opt.Value.RpcQueue,
            opt.Value.PrefetchCount, opt.Value.MaxPriority, _logger);
        _service.ReceivedAsync += ServiceReceivedAsync;
    }

    private async Task ServiceReceivedAsync(object sender, Proxy.RabbitMQ.EventArgsT<CallSession> e)
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("stop application start.");
        return Task.CompletedTask;
    }


    //public async Task StopAsync(CancellationToken cancellationToken)
    //{
    //    _logger.LogInformation("stop application start.");
    //    while (_busyFlag.IsHandling)
    //    {
    //        if (cancellationToken.IsCancellationRequested)
    //        {
    //            _logger.LogInformation("stop application, IsCancellationRequested");
    //            break;
    //        }

    //        try
    //        {
    //            await Task.Delay(1000, cancellationToken);
    //        }
    //        catch (OperationCanceledException)
    //        {
    //            _logger.LogInformation("stop application, OperationCanceledException");
    //            break;
    //        }
    //    }
    //    _logger.LogInformation("_service?.Dispose() start.");
    //    _service?.Dispose();
    //    _logger.LogInformation("stop application end.");
    //}
}