using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NetRpc.NamedPipe
{
    internal sealed class NamedPipeServiceGroup :
#if NETSTANDARD2_1
        IDisposableAsync
#else
        IDisposable
#endif
    {
        private readonly RequestHandler _requestHandler;
        private readonly List<NamedPipeServiceWrapper> _wrappers = new List<NamedPipeServiceWrapper>();

        public NamedPipeServiceGroup(RequestHandler requestHandler, IOptions<NamePipeServiceOptions> options)
        {
            _requestHandler = requestHandler;
            for (int i = 0; i < options.Value.MaxNumberOfServerInstances; i++)
            {
                var wrapper = new NamedPipeServiceWrapper(options.Value.MaxNumberOfServerInstances, options.Value.Name);
                _wrappers.Add(wrapper);
                wrapper.ReceivedAsync += WrapperReceivedAsync;
            }
        }

        private async Task WrapperReceivedAsync(object sender, EventArgsT<byte[]> @event)
        {
            await _requestHandler.HandleAsync(new NamedPipeServiceConnection((NamedPipeServiceWrapper) sender), ChannelType.NamedPipe);
        }

        public async Task WaitForConnectionAsync()
        {
            await Task.WhenAll(_wrappers.Select(i => i.WaitForConnectionAsync()));
        }

#if NETSTANDARD2_1
        public async Task DisposeAsync()
        {
             foreach (var w in _wrappers) 
                await w.DisposeAsync();
        }
#endif

        public void Dispose()
        {
            _wrappers.ForEach(i => i.Dispose());
        }
    }

    internal sealed class NamedPipeServiceWrapper :
#if NETSTANDARD2_1
        IDisposableAsync
#else
        IDisposable
#endif
    {
        private readonly NamedPipeServerStream _pipeServer;

        public NamedPipeServiceWrapper(int maxNumberOfServerInstances, string name)
        {
            _pipeServer = new NamedPipeServerStream(name, PipeDirection.In, maxNumberOfServerInstances, PipeTransmissionMode.Message);
        }

        public event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;

        public Task SendAsync(byte[] buffer, int offset, int count)
        {
            return _pipeServer.WriteAsync(buffer, offset, count);
        }

        public async Task WaitForConnectionAsync()
        {
            await _pipeServer.WaitForConnectionAsync();

#pragma warning disable 4014
            Task.Factory.StartNew(Receive, TaskCreationOptions.LongRunning);
#pragma warning restore 4014
        }

        private async void Receive()
        {
            var ms = new MemoryStream();
            var buffer = new byte[Helper.StreamBufferSize];
            var readCount = await _pipeServer.ReadAsync(buffer, 0, Helper.StreamBufferSize);
            while (_pipeServer.IsConnected && !_pipeServer.IsMessageComplete && readCount > 0)
                ms.Write(buffer, 0, readCount);

            await OnReceivedAsync(new EventArgsT<byte[]>(ms.ToArray()));
        }

        private Task OnReceivedAsync(EventArgsT<byte[]> e)
        {
            return ReceivedAsync.InvokeAsync(this, e);
        }

#if NETSTANDARD2_1
        public async Task DisposeAsync()
        {
            if (_pipeServer == null)
                return;
            await _pipeServer.DisposeAsync();
        }
#endif

        public void Dispose()
        {
            _pipeServer?.Dispose();
        }
    }
}