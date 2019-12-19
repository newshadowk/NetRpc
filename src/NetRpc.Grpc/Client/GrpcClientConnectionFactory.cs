using Grpc.Base;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientConnectionFactory : IClientConnectionFactory
    {
        private readonly ILogger _logger;
        private Client _client;

        public GrpcClientConnectionFactory(IOptionsMonitor<GrpcClientOptions> options, ILoggerFactory loggerFactory = null)
        {
            if (loggerFactory == null)
                _logger = NullLogger.Instance;
            else
                _logger = loggerFactory.CreateLogger("NetRpc");
            Reset(options.CurrentValue.Channel);
            options.OnChange(i => Reset(i.Channel));
        }

        public void Reset(Channel channel)
        {
            Dispose();
            _client = new Client(channel);
            _client.Connect();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public System.Threading.Tasks.ValueTask DisposeAsync()
        {
            return _client.DisposeAsync();
        }
#endif

        public IClientConnection Create()
        {
            return new GrpcClientConnection(_client, _logger);
        }
    }
}