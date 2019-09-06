using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientConnection : IDisposable
    {
        event EventHandler<EventArgsT<byte[]>> Received;

        Task SendAsync(byte[] buffer, bool isPost = false);

        Task StartAsync();
    }

    public interface IServiceConnection : IDisposable
    {
        event EventHandler<EventArgsT<byte[]>> Received;

        Task SendAsync(byte[] buffer);

        Task StartAsync();
    }
}