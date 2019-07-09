namespace NetRpc.Http
{
    public static class NetRpcManager
    {
        public static ServiceProxy CreateServiceProxy(int port, string rootPath, params object[] instances)
        {
            return new ServiceProxy(port, rootPath, instances);
        }
    }
}