namespace NetRpc
{
    public interface IOrphanClientProxyProvider
    {
        ClientProxy<TService> CreateProxy<TService>(string optionsName);
    }
}