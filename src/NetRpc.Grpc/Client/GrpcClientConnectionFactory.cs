using System.Collections.Generic;
using Proxy.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

#if NETCOREAPP3_1
using Grpc.Net.Client;
#else
using Grpc.Core;
#endif

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
            Reset(options.CurrentValue);
            options.OnChange(Reset);
        }

        public void Reset(GrpcClientOptions opt)
        {
            Dispose();
            
#if NETCOREAPP3_1
            if (opt.ChannelOptions == null)
                _client = new Client(GrpcChannel.ForAddress(opt.Url));
            else
                _client = new Client(GrpcChannel.ForAddress(opt.Url, opt.ChannelOptions));
#else
            if (string.IsNullOrEmpty(opt.PublicKey))
                _client = new Client(new Channel(opt.Host, opt.Port, ChannelCredentials.Insecure));
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
                _client = new Client(channel);
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
            return new GrpcClientConnection(_client, _logger);
        }
    }
}