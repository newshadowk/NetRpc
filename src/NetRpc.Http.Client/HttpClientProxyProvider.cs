using System;
using Microsoft.Extensions.Options;

namespace NetRpc.Http.Client
{
    public class HttpClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<HttpClientOptions> _httpClientOptions;
        private readonly IOptionsMonitor<NetRpcClientOption> _netRpcClientOption;
        private readonly IServiceProvider _serviceProvider;

        public HttpClientProxyProvider(IOptionsMonitor<HttpClientOptions> httpClientOptions, IOptionsMonitor<NetRpcClientOption> netRpcClientOption,
            IServiceProvider serviceProvider)
        {
            _httpClientOptions = httpClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _serviceProvider = serviceProvider;
        }

        protected override ClientProxy<TService> CreateInner<TService>(string name)
        {
            var options = _httpClientOptions.Get(name);
            if (options.IsPropertiesDefault())
                return null;

            var f = new HttpOnceCallFactory(options);
            var clientProxy = new ClientProxy<TService>(f, _netRpcClientOption, _serviceProvider, name);
            return clientProxy;
        }
    }
}