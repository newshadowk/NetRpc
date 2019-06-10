using System.Collections.Generic;
using Grpc.Base;
using Grpc.Core;

namespace NetRpc.Grpc
{
    public sealed class ServiceProxy
    {
        private readonly List<ServerPort> _ports;
        private Service _service;
        private readonly MessageCallImpl _messageCallImpl;

        public ServiceProxy(List<ServerPort> ports, object[] instances)
        {
            _ports = ports;
            _messageCallImpl = new MessageCallImpl(instances);
        }

        public void UseMiddleware<TMiddleware>(params object[] args) where TMiddleware : MiddlewareBase
        {
            _messageCallImpl.UseMiddleware<TMiddleware>(args);
        }

        public void Open()
        {
            _service = new Service(_ports, _messageCallImpl);
            _service.Open();
        }
    }
}