using System.Collections.Generic;
using Grpc.Core;

namespace Grpc.Base
{
    public sealed class Service
    {
        private readonly List<ServerPort> _ports;
        private readonly MessageCall.MessageCallBase _messageCall;

        public Service(List<ServerPort> ports, MessageCall.MessageCallBase messageCall)
        {
            _ports = ports;
            _messageCall = messageCall;
        }

        public void Open()
        {
            Server server = new Server
            {
                Services = {MessageCall.BindService(_messageCall)}
            };

            foreach (var port in _ports)
                server.Ports.Add(port);

            server.Start();
        }
    }
}