using System;
using Grpc.Core;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientProxy<TService> : ClientProxy<TService>
    {
        public GrpcClientProxy(IConnectionFactory factory, bool isWrapFaultException, int timeoutInterval, int hearbeatInterval) : base(factory, isWrapFaultException, timeoutInterval, hearbeatInterval)
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