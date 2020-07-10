using System;
using System.Threading.Tasks;

namespace NetRpc.NamedPipe
{
    internal sealed class NamedPipeServiceConnection : IServiceConnection
    {
        private readonly NamedPipeServiceWrapper _wrapper;


        public NamedPipeServiceConnection(NamedPipeServiceWrapper wrapper)
        {
            _wrapper = wrapper;
        }

#if NETSTANDARD2_1
        public Task DisposeAsync()
        {
            return _wrapper.DisposeAsync();
        }
#endif

        public void Dispose()
        {
            _wrapper.Dispose();
        }

        public event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;

        public Task SendAsync(byte[] buffer)
        {
            _wrapper.SendAsync(buffer, 0, buffer.Length);
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }
    }
}