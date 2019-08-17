using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Grpc.Base
{
    public sealed class Service : IDisposable
    {
        private readonly Server _server;
        private volatile bool _disposed;

        public Service(List<ServerPort> ports, MessageCall.MessageCallBase messageCall)
        {
            _server = new Server
            {
                Services = { MessageCall.BindService(messageCall) }
            };

            foreach (var port in ports)
                _server.Ports.Add(port);
        }

        public void Open()
        {
            _server.Start();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _server.KillAsync().Wait();
            _disposed = true;
        }
    }
}