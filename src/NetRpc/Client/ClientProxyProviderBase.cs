using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NetRpc
{
    public abstract class ClientProxyProviderBase : IClientProxyProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<object>> _caches = new ConcurrentDictionary<string, Lazy<object>>(StringComparer.Ordinal);

        protected abstract ClientProxy<TService> CreateInner<TService>(string name);

        public ClientProxy<TService> CreateProxy<TService>(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var handler = (ClientProxy<TService>)_caches.GetOrAdd(name, new Lazy<object>(() => CreateInner<TService>(name), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            return handler;
        }
    }
}