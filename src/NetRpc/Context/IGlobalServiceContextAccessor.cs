namespace NetRpc
{
    public interface IGlobalServiceContextAccessor
    {
        ServiceContext Context { get; set; }
    }
}