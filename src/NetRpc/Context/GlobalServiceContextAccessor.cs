namespace NetRpc
{
    public class GlobalServiceContextAccessor : IGlobalServiceContextAccessor
    {
        public ActionExecutingContext Context
        {
            get => GlobalServiceContext.Context;
            set => GlobalServiceContext.Context = value;
        }
    }
}