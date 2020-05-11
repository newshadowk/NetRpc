using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<RabbitMQClientOptions> _rabbitMQClientOptions;
        private readonly IOptionsMonitor<NetRpcClientOption> _netRpcClientOption;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitMQClientProxyProvider(IOptionsMonitor<RabbitMQClientOptions> rabbitMQClientOptions, IOptionsMonitor<NetRpcClientOption> netRpcClientOption,
            IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _rabbitMQClientOptions = rabbitMQClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        protected override ClientProxy<TService> CreateProxyInner<TService>(string optionsName)
        {
            var options = _rabbitMQClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;
            
            var f = new RabbitMQClientConnectionFactory(new SimpleOptions<RabbitMQClientOptions>(options), _loggerFactory);
            var clientProxy = new ClientProxy<TService>(f, new SimpleOptions<NetRpcClientOption>(_netRpcClientOption.CurrentValue), _serviceProvider, _loggerFactory);
            return clientProxy;
        }
    }

    public class OrphanRabbitMQClientProxyProvider : IOrphanClientProxyProvider
    {
        private readonly IOptionsMonitor<RabbitMQClientOptions> _rabbitMQClientOptions;
        private readonly IOptionsMonitor<NetRpcClientOption> _netRpcClientOption;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public OrphanRabbitMQClientProxyProvider(IOptionsMonitor<RabbitMQClientOptions> rabbitMQClientOptions, IOptionsMonitor<NetRpcClientOption> netRpcClientOption,
            IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _rabbitMQClientOptions = rabbitMQClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        public ClientProxy<TService> CreateProxy<TService>(string optionsName)
        {
            var options = _rabbitMQClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;

            var f = new RabbitMQClientConnectionFactory(new SimpleOptions<RabbitMQClientOptions>(options), _loggerFactory);
            var clientProxy = new ClientProxy<TService>(f, new SimpleOptions<NetRpcClientOption>(_netRpcClientOption.CurrentValue), _serviceProvider, _loggerFactory);
            return clientProxy;
        }
    }
}