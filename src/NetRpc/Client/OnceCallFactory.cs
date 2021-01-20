using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetRpc
{
    internal sealed class OnceCallFactory : IOnceCallFactory
    {
        private readonly IClientConnectionFactory _factory;
        private readonly ILogger _logger;

        public OnceCallFactory(IClientConnectionFactory factory, ILoggerFactory loggerFactory)
        {
            _factory = factory;
            _logger = loggerFactory.CreateLogger("NetRpc");
        }

        public async Task<IOnceCall> CreateAsync(int timeoutInterval, bool isRetry)
        {
            await using var convert = new BufferClientOnceApiConvert(_factory.Create(isRetry), _logger);
            return new OnceCall(convert, timeoutInterval, _logger);
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}