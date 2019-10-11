namespace NetRpc
{
    public interface IClientProxyProvider
    {
        ClientProxy<TService> CreateProxy<TService>(string name);
    }
}