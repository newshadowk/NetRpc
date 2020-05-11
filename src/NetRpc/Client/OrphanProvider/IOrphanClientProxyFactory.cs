namespace NetRpc
{
    public interface IOrphanClientProxyFactory
    {
        IClientProxy<TService> CreateProxy<TService>(string optionsName);
    }
}