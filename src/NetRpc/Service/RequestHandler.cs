using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;

namespace NetRpc;

public sealed class RequestHandler
{
    private readonly ILogger _logger;
    private readonly MiddlewareBuilder _middlewareBuilder;
    private readonly IServiceProvider _serviceProvider;

    public RequestHandler(IServiceProvider serviceProvider, ILoggerFactory factory, MiddlewareBuilder middlewareBuilder)
    {
        _serviceProvider = serviceProvider;
        _logger = factory.CreateLogger("NetRpc");
        _middlewareBuilder = middlewareBuilder;
    }

    public async Task HandleAsync(IServiceConnection connection, ChannelType channelType)
    {
        await using var convert = new BufferServiceOnceApiConvert(connection, _logger);
        await HandleAsync(convert, channelType);
    }

    public async Task HandleAsync(IServiceOnceApiConvert convert, ChannelType channelType)
    {
        try
        {
            var contractOptions = _serviceProvider.GetRequiredService<IOptions<ContractOptions>>();
            var contextAccessor = _serviceProvider.GetRequiredService<IActionExecutingContextAccessor>();

            using var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            GlobalServiceProvider.Provider = _serviceProvider;
            GlobalServiceProvider.ScopeProvider = scope.ServiceProvider;
            var instances = scope.ServiceProvider.GetContractInstances(contractOptions.Value);
            var onceTransfer = new ServiceOnceTransfer(instances,
                scope.ServiceProvider,
                convert,
                _middlewareBuilder,
                contextAccessor,
                channelType,
                _logger);
            if (await onceTransfer.StartAsync())
                await onceTransfer.HandleRequestAsync();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, null);
            throw;
        }
    }
}