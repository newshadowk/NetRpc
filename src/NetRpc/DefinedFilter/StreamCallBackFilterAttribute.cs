namespace NetRpc
{
    public class StreamCallBackAttribute : ActionFilterAttribute
    {
        private readonly int _progressCount;

        public StreamCallBackAttribute(int progressCount)
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