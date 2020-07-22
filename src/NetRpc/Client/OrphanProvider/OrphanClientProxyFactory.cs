using System.Collections.Generic;
using System.Linq;

namespace NetRpc
{
    public class OrphanClientProxyFactory : IOrphanClientProxyFactory
    {
        private readonly List<IOrphanClientProxyProvider> _providers;

        public OrphanClientProxyFactory(IEnumerable<IOrphanClientProxyProvider> providers)
        {
            _providers = providers.ToList();
        }

        public IClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class
        {
            foreach (var p in _providers)
            {
                var client = p.CreateProxy<TService>(optionsName);
                if (client != null)
                    return client;
            }

            return null;
        }
    }
}