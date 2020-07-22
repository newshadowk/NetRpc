using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Proxy.Grpc
{
    public class Service : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
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

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            //have not deadlock issue.
            _server.KillAsync().Wait();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            await _server.KillAsync();
            _disposed = true;
        }
#endif
    }
}