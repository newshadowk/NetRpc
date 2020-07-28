namespace NetRpc
{
    public class StreamCallBackFilterAttribute : ActionFilterAttribute
    {
        private readonly int _progressCount;

        public StreamCallBackFilterAttribute(int progressCount)
        {
            _progressCount = progressCount;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Helper.ConvertStreamProgress(context, _progressCount);
            base.OnActionExecuting(context);
        }
    }
}