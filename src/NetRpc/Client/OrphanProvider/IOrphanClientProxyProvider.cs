namespace NetRpc;

/// <summary>
/// Create a ClientProxy without dispose while scope ends.
/// </summary>
public interface IOrphanClientProxyProvider
{
    ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class;
}