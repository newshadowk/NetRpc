using System;

namespace NetRpc
{
    public interface IConnectionFactory : IDisposable
    {
        IConnection Create();
    }
}