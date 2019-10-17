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

        protected override ClientProxy<TService> CreateInner<TService>(string name)
        {
            var options = _grpcClientOptions.Get(name);
            if (options.IsPropertiesDefault())
                return null;

            var f = new GrpcClientConnectionFactory(new SimpleOptionsMonitor<GrpcClientOptions>(options));
            var clientProxy = new GrpcClientProxy<TService>(f, _netRpcClientOption, _serviceProvider, name);
            return clientProxy;
        }
    }
}