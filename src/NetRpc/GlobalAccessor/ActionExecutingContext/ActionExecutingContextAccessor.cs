namespace NetRpc
{
    public class ActionExecutingContextAccessor : IActionExecutingContextAccessor
    {
        public ActionExecutingContext Context
        {
            get => GlobalActionExecutingContext.Context;
            set => GlobalActionExecutingContext.Context = value;
        }
    }
}