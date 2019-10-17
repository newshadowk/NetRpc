using System;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientProxy<TService> : ClientProxy<TService>
    {
        public GrpcClientProxy(IClientConnectionFactory factory, IOptionsMonitor<NetRpcClientOption> options, IServiceProvider serviceProvider, string optionsName = null)
            : base(factory, options, serviceProvider, optionsName)
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