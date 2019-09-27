namespace NetRpc.Http.Client
{
    public static class NetRpcManager
    {
        public static ClientProxy<TService> CreateClientProxy<TService>(HttpClientOptions options, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new ClientProxy<TService>(new HttpOnceCallFactory(options),
                new SimpleOptionsMonitor<NetRpcClientOption>(new NetRpcClientOption
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ), null);
        }
    }
}