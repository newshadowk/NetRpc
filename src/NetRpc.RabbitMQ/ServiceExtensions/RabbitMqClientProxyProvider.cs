using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace NetRpc.RabbitMQ
{
    public class RabbitMqClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<RabbitMQClientOptions> _rabbitMqClientOptions;
        private readonly IOptionsMonitor<NetRpcClientOption> _netRpcClientOption;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitMqClientProxyProvider(IOptionsMonitor<RabbitMQClientOptions> rabbitMQClientOptions, IOptionsMonitor<NetRpcClientOption> netRpcClientOption,
            IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _rabbitMqClientOptions = rabbitMQClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        protected override ClientProxy<TService> CreateProxyInner<TService>(string optionsName)
        {
            var options = _rabbitMqClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;
            
            var f = new RabbitMQClientConnectionFactory(new SimpleOptionsMonitor<RabbitMQClientOptions>(options), _loggerFactory);
            var clientProxy = new ClientProxy<TService>(f, _netRpcClientOption, _serviceProvider, _loggerFactory);
            return clientProxy;
        }
    }
}