using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NetRpc
{
    public interface IClientProxyProvider
    {
        ClientProxy<TService> CreateProxy<TService>(string name);
    }

    public interface IClientProxyFactory
    {
        ClientProxy<TService> CreateProxy<TService>(string name);
    }

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

    public class ClientProxyFactory : IClientProxyFactory
    {
        private readonly List<IClientProxyProvider> _providers;

        public ClientProxyFactory(IEnumerable<IClientProxyProvider> providers)
        {
            _providers = providers.ToList();
        }

        public ClientProxy<TService> CreateProxy<TService>(string name)
        {
            foreach (var p in _providers)
            {
                var client = p.CreateProxy<TService>(name);
                if (client != null)
                    return client;
            }

            return null;
        }
    }
}