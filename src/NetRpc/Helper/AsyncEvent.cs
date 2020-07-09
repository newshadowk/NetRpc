using System.Threading.Tasks;

namespace System
{
    public delegate Task AsyncEventHandler<in TEvent>(object sender, TEvent @event) where TEvent : EventArgs;

    public static class AsyncEventHandlerExtensions
    {
        public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> eventHandler, object sender, TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            if (eventHandler == null)
                return;
            var delegateArray = eventHandler.GetInvocationList();
            foreach (var t in delegateArray)
                await ((AsyncEventHandler<TEventArgs>)t)(sender, eventArgs).ConfigureAwait(false);
        }
    }
}