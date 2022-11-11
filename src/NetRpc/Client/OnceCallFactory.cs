using Microsoft.Extensions.Logging;

namespace NetRpc;

internal sealed class OnceCallFactory : IOnceCallFactory
{
    private readonly IClientConnectionFactory _factory;
    private readonly ILogger _logger;

    public OnceCallFactory(IClientConnectionFactory factory, ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger("NetRpc");
    }

    public Task<IOnceCall> CreateAsync(int timeoutInterval, bool isRetry)
    {
        return Task.FromResult<IOnceCall>(new OnceCall(new BufferClientOnceApiConvert(_factory.Create(isRetry), _logger), timeoutInterval, _logger));
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}