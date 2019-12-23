using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Proxy.Grpc
{
#if NETSTANDARD2_1 || NETCOREAPP3_1
    public class Service : IDisposable, IAsyncDisposable
#else
    public sealed class Service : IDisposable
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
            _server.KillAsync().Wait();
            _disposed = true;
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