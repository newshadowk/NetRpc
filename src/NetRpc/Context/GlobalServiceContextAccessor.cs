namespace NetRpc
{
    public class GlobalServiceContextAccessor : IGlobalServiceContextAccessor
    {
        public ServiceContext Context
        {
            get => GlobalServiceContext.Context;
            set => GlobalServiceContext.Context = value;
        }
    }
}