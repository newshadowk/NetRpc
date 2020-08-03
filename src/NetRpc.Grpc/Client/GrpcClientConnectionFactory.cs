using System;
using System.Collections.Generic;
using Proxy.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#if NETCOREAPP3_1
using System;
using Grpc.Net.Client;
#else
using Grpc.Core;
#endif

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
            Console.WriteLine($"!!!_client Reset, {opt}");

#if NETCOREAPP3_1
            var host = new Uri(opt.Url!).Host;
            var port = new Uri(opt.Url!).Port;
            _client = new Client(GrpcChannel.ForAddress(opt.Url!, opt.ChannelOptions!), host, port, opt.ToString());
#else
            if (string.IsNullOrEmpty(opt.PublicKey))
                _client = new Client(new Channel(opt.Host, opt.Port, ChannelCredentials.Insecure), opt.Host!, opt.Port, opt.ToString());
            else
            {
                Channel channel;
                var ssl = new SslCredentials(opt.PublicKey);
                if (string.IsNullOrEmpty(opt.SslTargetName))
                    channel = new Channel(opt.Host, opt.Port, ssl);
                else
                {
                    var options = new List<ChannelOption>();
                    options.Add(new ChannelOption(ChannelOptions.SslTargetNameOverride, opt.SslTargetName));
                    channel = new Channel(opt.Host, opt.Port, ssl, options);
                }
                _client = new Client(channel, opt.Host!, opt.Port, opt.ToString());
            }
#endif
            _client.Connect();
        }

        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            //connection dispose before client dispose.
            if (_connection != null)
                await _connection.DisposeFinishAsync();

            if (_client != null)
                await _client.DisposeAsync();
        }

        public IClientConnection Create()
        {
            _connection = new GrpcClientConnection(_client!, _logger);
            return _connection;
        }
    }
}