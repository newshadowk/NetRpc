using System;

namespace NetRpc
{
    public interface IClientConnectionFactory : IDisposable
    {
        IClientConnection Create(bool isRetry);
    }
}