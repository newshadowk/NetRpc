using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public abstract class ClientProxyProviderBase : IClientProxyProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<object?>> _caches = new ConcurrentDictionary<string, Lazy<object?>>(StringComparer.Ordinal);

        protected abstract ClientProxy<TService>? CreateProxyInner<TService>(string optionsName) where TService : class;

        public ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class
        {
            var key = $"{optionsName}_{typeof(TService).FullName}";
            var clientProxy = (ClientProxy<TService>?)_caches.GetOrAdd(key, new Lazy<object?>(() => 
                CreateProxyInner<TService>(optionsName), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            return clientProxy;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
                await DisposeManaged();
        }

        private async ValueTask DisposeManaged()
        {
            foreach (var proxy in _caches.Values) 
                await ((IClientProxy) proxy.Value!)!.DisposeAsync();
        }
    }
}