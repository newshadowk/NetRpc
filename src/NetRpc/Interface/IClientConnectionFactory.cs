using System;

namespace NetRpc
{
    public interface IClientConnectionFactory : IAsyncDisposable
    {
        IClientConnection Create();
    }
}