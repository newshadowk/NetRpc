namespace NetRpc;

public interface IOrphanClientProxyProvider
{
    ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class;
}