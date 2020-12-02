using System.Threading;

namespace NetRpc
{
    public interface IActionExecutingContextAccessor
    {
        ActionExecutingContext? Context { get; set; }
    }

    public static class GlobalActionExecutingContext
    {
        private static readonly AsyncLocal<ActionExecutingContext> Local = new();

        public static ActionExecutingContext? Context
        {
            get => Local.Value;
            set => Local.Value = value!;
        }
    }

    public class ActionExecutingContextAccessor : IActionExecutingContextAccessor
    {
        public ActionExecutingContext? Context
        {
            get => GlobalActionExecutingContext.Context;
            set => GlobalActionExecutingContext.Context = value;
        }

        public static readonly ActionExecutingContextAccessor Default = new();
    }
}