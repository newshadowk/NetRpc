namespace NetRpc
{
    public interface IRpcContextAccessor
    {
        RpcContext Context { get; set; }
    }
}