namespace NetRpc;

public class ClientProxyFactory : IClientProxyFactory
{
    private readonly List<IClientProxyProvider> _providers;

    public ClientProxyFactory(IEnumerable<IClientProxyProvider> providers)
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