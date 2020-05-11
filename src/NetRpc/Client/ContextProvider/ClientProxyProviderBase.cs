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
            var clientProxy = (ClientProxy<TService>)_caches.GetOrAdd(key, new Lazy<object>(() => 
                CreateProxyInner<TService>(optionsName), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            return clientProxy;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                DisposeManaged();
        }

        private void DisposeManaged()
        {
            foreach (var proxy in _caches.Values) 
                ((IClientProxy) proxy.Value).Dispose();
        }
    }
}