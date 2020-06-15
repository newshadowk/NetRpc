using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientProxy<TService> : ClientProxy<TService>
    {
        public GrpcClientProxy(IClientConnectionFactory factory,
            IOptions<NetRpcClientOption> clientOptions,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory, string optionsName = null)
            : base(factory,
                clientOptions,
                clientMiddlewareOptions,
                actionExecutingContextAccessor,
                serviceProvider,
                loggerFactory,
                optionsName)
        {
            ExceptionInvoked += GrpcClientProxy_ExceptionInvoked;
        }

        private void GrpcClientProxy_ExceptionInvoked(object sender, EventArgsT<Exception> e)
        {
            if (e.Value is RpcException)
                IsConnected = false;
        }
    }
}