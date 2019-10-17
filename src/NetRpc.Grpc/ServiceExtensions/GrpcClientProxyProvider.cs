using System;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public class GrpcClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<GrpcClientOptions> _grpcClientOptions;
        private readonly IOptionsMonitor<NetRpcClientOption> _netRpcClientOption;
        private readonly IServiceProvider _serviceProvider;

        public GrpcClientProxyProvider(IOptionsMonitor<GrpcClientOptions> grpcClientOptions, IOptionsMonitor<NetRpcClientOption> netRpcClientOption,
            IServiceProvider serviceProvider)
        {
            _grpcClientOptions = grpcClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _serviceProvider = serviceProvider;
        }

        protected override ClientProxy<TService> CreateProxyInner<TService>(string optionsName)
        {
            var options = _grpcClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;

            var f = new GrpcClientConnectionFactory(new SimpleOptionsMonitor<GrpcClientOptions>(options));
            var clientProxy = new GrpcClientProxy<TService>(f, _netRpcClientOption, _serviceProvider, optionsName);
            return clientProxy;
        }
    }
}