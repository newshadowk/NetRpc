using System;
using System.Threading.Tasks;

namespace NetRpc
{
#if NETSTANDARD2_1
    public interface IClientConnection : IDisposable, IAsyncDisposable
#else
    public interface IClientConnection : IDisposable
#endif
    {
        event EventHandler<EventArgsT<byte[]>> Received;

        Task SendAsync(byte[] buffer, bool isPost = false);

        Task StartAsync();
    }
#if NETSTANDARD2_1
    public interface IServiceConnection : IDisposable, IAsyncDisposable
#else
    public interface IServiceConnection : IDisposable
#endif
    {
        event EventHandler<EventArgsT<byte[]>> Received;

        Task SendAsync(byte[] buffer);

        Task StartAsync();
    }
}