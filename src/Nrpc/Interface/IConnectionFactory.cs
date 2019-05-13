using System;

namespace Nrpc
{
    public interface IConnectionFactory : IDisposable
    {
        IConnection Create();
    }
}