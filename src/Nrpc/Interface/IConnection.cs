using System;
using System.Threading.Tasks;

namespace Nrpc
{
    public interface IConnection : IDisposable
    {
        event EventHandler<EventArgsT<byte[]>> Received;

        Task Send(byte[] buffer);

        void Start();
    }
}