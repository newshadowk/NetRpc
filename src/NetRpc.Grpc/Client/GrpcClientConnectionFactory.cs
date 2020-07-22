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

        public GrpcClientConnectionFactory(IOptions<GrpcClientOptions> options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("NetRpc");
            Reset(options.Value);
        }

        private void Reset(GrpcClientOptions opt)
        {

#if NETCOREAPP3_1
            var host = new Uri(opt.Url).Host;
            var port = new Uri(opt.Url).Port;
            _client = new Client(GrpcChannel.ForAddress(opt.Url, opt.ChannelOptions), host, port, opt.ToString());
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
            return new GrpcClientConnection(_client!, _logger);
        }
    }
}