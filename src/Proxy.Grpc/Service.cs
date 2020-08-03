using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Proxy.Grpc
{
    public class Service : IAsyncDisposable
    {
        private readonly Server _server;
        private volatile bool _disposed;

        public Service(List<ServerPort> ports, MessageCall.MessageCallBase messageCall)
        {
            _server = new Server
            {
                Services = {MessageCall.BindService(messageCall)}
            };

            foreach (var port in ports)
                _server.Ports.Add(port);
        }

        public void Open()
        {
            _server.Start();
        }

        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            await _server.KillAsync();
            _disposed = true;
        }
    }
}