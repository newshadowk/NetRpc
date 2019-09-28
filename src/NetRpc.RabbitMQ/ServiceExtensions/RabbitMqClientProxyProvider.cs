using System;
using Microsoft.Extensions.Options;

namespace NetRpc.RabbitMQ
{
    public class RabbitMqClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<RabbitMQClientOptions> _rabbitMqClientOptions;
        private readonly IOptionsMonitor<NetRpcClientOption> _netRpcClientOption;
        private readonly IServiceProvider _serviceProvider;

        public RabbitMqClientProxyProvider(IOptionsMonitor<RabbitMQClientOptions> rabbitMQClientOptions, IOptionsMonitor<NetRpcClientOption> netRpcClientOption,
            IServiceProvider serviceProvider)
        {
            _rabbitMqClientOptions = rabbitMQClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _serviceProvider = serviceProvider;
        }

        protected override ClientProxy<TService> CreateInner<TService>(string name)
        {
            var options = _rabbitMqClientOptions.Get(name);
            if (options.IsPropertiesDefault())
                return null;
            
            var f = new RabbitMQClientConnectionFactory(new SimpleOptionsMonitor<RabbitMQClientOptions>(options));
            var clientProxy = new ClientProxy<TService>(f, _netRpcClientOption, _serviceProvider);
            return clientProxy;
        }
    }
}