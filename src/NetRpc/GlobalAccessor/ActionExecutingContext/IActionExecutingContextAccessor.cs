namespace NetRpc
{
    public interface IActionExecutingContextAccessor
    {
        ActionExecutingContext Context { get; set; }
    }
}