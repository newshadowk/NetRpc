using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IConnection : IDisposable
    {
        event EventHandler<EventArgsT<byte[]>> Received;

        Task SendAsync(byte[] buffer);

        void Start();
    }
}