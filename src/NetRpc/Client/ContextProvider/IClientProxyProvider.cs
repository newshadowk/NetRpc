using System;

namespace NetRpc
{
    public interface IClientProxyProvider : IAsyncDisposable
    {
        ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class;
    }
}