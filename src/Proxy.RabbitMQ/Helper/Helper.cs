using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    internal static class Helper
    {
        public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> @event, object sender, TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            var handler = @event;
            if (handler == null)
                return;

            var invocationList = handler.GetInvocationList();
            Task[] handlerTasks = new Task[invocationList.Length];
            for (var i = 0; i < invocationList.Length; i++)
                handlerTasks[i] = ((AsyncEventHandler<TEventArgs>)invocationList[i])(sender, eventArgs);

            await Task.WhenAll(handlerTasks);
        }
    }
}