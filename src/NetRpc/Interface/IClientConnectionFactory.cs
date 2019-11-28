using System;

namespace NetRpc
{
#if NETSTANDARD2_1
    public interface IClientConnectionFactory : IDisposable, IAsyncDisposable
#else
    public interface IClientConnectionFactory : IDisposable
#endif
    {
        IClientConnection Create();
    }
}