using Grpc.Core;

namespace Grpc.Base
{
    public sealed class Service
    {
        private readonly string _host;
        private readonly int _port;
        private readonly MessageCall.MessageCallBase _messageCall;

        public Service(string host, int port, MessageCall.MessageCallBase messageCall)
        {
            _host = host;
            _port = port;
            _messageCall = messageCall;
        }

        public void Open()
        {
            Server server = new Server
            {
                Services = { MessageCall.BindService(_messageCall) },
                Ports = {new ServerPort(_host, _port, ServerCredentials.Insecure)}
            };
            server.Start();
        }
    }
}