using Microsoft.Extensions.Logging;

namespace NetRpc.OpenTracing
{
    public class SpanLoggerProvider : ILoggerProvider
    {
        private readonly SpanLogger _logger = new();

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }
    }
}