namespace NetRpc
{
    public interface IGlobalServiceContextAccessor
    {
        ActionExecutingContext Context { get; set; }
    }
}