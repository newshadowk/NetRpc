using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NetRpc
{
    public sealed class RequestHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ChannelType _channelType;
        private readonly MiddlewareBuilder _middlewareBuilder;

        public RequestHandler(IServiceProvider serviceProvider, ChannelType channelType)
        {
            _serviceProvider = serviceProvider;
            _channelType = channelType;
            var middlewareOptions = _serviceProvider.GetService<IOptions<MiddlewareOptions>>().Value;
            _middlewareBuilder = new MiddlewareBuilder(middlewareOptions, serviceProvider);
        }

        public async Task HandleAsync(IServiceConnection connection)
        {
            await HandleAsync(new BufferServiceOnceApiConvert(connection));
        }

        public async Task HandleAsync(IServiceOnceApiConvert convert)
        {
            var contractOptions = _serviceProvider.GetRequiredService<IOptions<ContractOptions>>();
            var rpcContextAccessor = _serviceProvider.GetRequiredService<IGlobalServiceContextAccessor>();
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var instances = scope.ServiceProvider.GetContractInstances(contractOptions.Value);
                var onceTransfer = new ServiceOnceTransfer(instances, scope.ServiceProvider, convert, _middlewareBuilder, rpcContextAccessor, _channelType);
                await onceTransfer.StartAsync();
                await onceTransfer.HandleRequestAsync();
            }
        }
    }
}