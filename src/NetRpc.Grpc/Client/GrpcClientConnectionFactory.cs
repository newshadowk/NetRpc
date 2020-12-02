using System;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxy.Grpc;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientConnectionFactory : IClientConnectionFactory
    {
        private readonly ILogger _logger;
        private Client? _client;
        private GrpcClientConnection? _connection;

        public GrpcClientConnectionFactory(IOptions<GrpcClientOptions> options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("NetRpc");
            Reset(options.Value);
        }

        private void Reset(GrpcClientOptions opt)
        {
            var host = new Uri(opt.Url!).Host;
            var port = new Uri(opt.Url!).Port;
            _client = new Client(GrpcChannel.ForAddress(opt.Url!, opt.ChannelOptions!), host, port, opt.ToString());
            _client.Connect();
        }

        public IClientConnection Create()
        {
            _connection = new GrpcClientConnection(_client!, _logger);
            return _connection;
        }

        public async void Dispose()
        {
            //connection dispose before client dispose.
            if (_connection != null)
                await _connection.DisposeFinishAsync();

            if (_client != null)
                await _client.DisposeAsync();
        }
    }
}