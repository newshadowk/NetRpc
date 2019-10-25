using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NetRpc
{
    public abstract class ClientProxyProviderBase : IClientProxyProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<object>> _caches = new ConcurrentDictionary<string, Lazy<object>>(StringComparer.Ordinal);

        protected abstract ClientProxy<TService> CreateProxyInner<TService>(string optionsName);

        public ClientProxy<TService> CreateProxy<TService>(string optionsName)
        {
            var key = $"{optionsName}_{typeof(TService).FullName}";
            var handler = (ClientProxy<TService>)_caches.GetOrAdd(key, new Lazy<object>(() => CreateProxyInner<TService>(optionsName), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            return handler;
        }
    }
}