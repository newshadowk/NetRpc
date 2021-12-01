using System;

namespace NetRpc;

public interface IClientProxyProvider : IDisposable
{
    ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class;
}