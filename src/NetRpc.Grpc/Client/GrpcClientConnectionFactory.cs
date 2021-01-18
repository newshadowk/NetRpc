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
        private readonly Client _client;
        private readonly SyncList<GrpcClientConnection> _connections = new();

        public GrpcClientConnectionFactory(IOptions<GrpcClientOptions> options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("NetRpc");
            var opt = options.Value;
            var host = new Uri(opt.Url!).Host;
            var port = new Uri(opt.Url!).Port;
            _client = new Client(GrpcChannel.ForAddress(opt.Url!, opt.ChannelOptions!), host, port, opt.ToString());
            _client.Connect();
        }

        public IClientConnection Create()
        {
            var connection = new GrpcClientConnection(_client!, _logger);
            connection.Finished += (s, _) => _connections.Remove((GrpcClientConnection) s!);
            _connections.Add(connection);
            return connection;
        }


        public async void Dispose()
        {
            //connection dispose before client dispose.
            foreach (var c in _connections.ToArray())
                await c.DisposeFinishAsync();

            await _client.DisposeAsync();
        }
    }
}