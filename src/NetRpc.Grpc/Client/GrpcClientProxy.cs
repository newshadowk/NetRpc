using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientProxy<TService> : ClientProxy<TService> where TService : class
    {
        public GrpcClientProxy(IClientConnectionFactory factory,
            IOptions<NClientOptions> nClientOptions,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            string? optionsName = null)
            : base(factory,
                nClientOptions,
                clientMiddlewareOptions,
                actionExecutingContextAccessor,
                serviceProvider,
                loggerFactory,
                optionsName)
        {
            ExceptionInvoked += GrpcClientProxy_ExceptionInvoked;
        }

        private void GrpcClientProxy_ExceptionInvoked(object? sender, EventArgsT<Exception> e)
        {
            if (e.Value is DisconnectedException)
                IsConnected = false;
        }
    }
}