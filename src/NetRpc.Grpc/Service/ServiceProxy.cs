using Grpc.Base;

namespace NetRpc.Grpc
{
    public sealed class ServiceProxy
    {
        private readonly string _host;
        private readonly int _port;
        private Service _service;
        private readonly MessageCallImpl _messageCallImpl;

        public ServiceProxy(string host, int port, object[] instances)
        {
            _host = host;
            _port = port;
            _messageCallImpl = new MessageCallImpl(instances);
        }

        public void UseMiddleware<TMiddleware>(params object[] args) where TMiddleware : MiddlewareBase
        {
            _messageCallImpl.UseMiddleware<TMiddleware>(args);
        }

        public void Open()
        {
            _service = new Service(_host, _port, _messageCallImpl);
            _service.Open();
        }
    }
}