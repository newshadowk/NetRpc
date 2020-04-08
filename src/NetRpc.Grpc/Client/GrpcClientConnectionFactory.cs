using System;
using System.Collections.Generic;
using Proxy.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private Client _client;
        private readonly IDisposable _optionDisposable;

        public GrpcClientConnectionFactory(IOptionsMonitor<GrpcClientOptions> options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("NetRpc");
            Reset(options.CurrentValue);
            _optionDisposable = options.OnChange(Reset);
        }

        public void Reset(GrpcClientOptions opt)
        {
            Dispose();
            
#if NETCOREAPP3_1
            var host = new Uri(opt.Url).Host;
            var port = new Uri(opt.Url).Port;
            
            if (opt.ChannelOptions == null)
                _client = new Client(GrpcChannel.ForAddress(opt.Url), host, port, opt.ToString());
            else
                _client = new Client(GrpcChannel.ForAddress(opt.Url, opt.ChannelOptions), host, port, opt.ToString());
#else
            if (string.IsNullOrEmpty(opt.PublicKey))
                _client = new Client(new Channel(opt.Host, opt.Port, ChannelCredentials.Insecure), opt.Host, opt.Port, opt.ToString());
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
                _client = new Client(channel, opt.Host, opt.Port, opt.ToString());
            }
#endif
            _client.Connect();
        }

        public void Dispose()
        {
            _optionDisposable.Dispose();
            _client?.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public System.Threading.Tasks.ValueTask DisposeAsync()
        {
            _optionDisposable.Dispose();
            return _client.DisposeAsync();
        }
#endif

        public IClientConnection Create()
        {
            return new GrpcClientConnection(_client, _logger);
        }
    }
}