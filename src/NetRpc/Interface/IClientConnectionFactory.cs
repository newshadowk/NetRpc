using System;

namespace NetRpc
{
    public interface IClientConnectionFactory : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
    {
        IClientConnection Create();
    }
}