using System;
using System.Threading.Tasks;

namespace RabbitMQ.Base
{
    internal static class Helper
    {
        public static async Task InvokeAsync<TEventArgs>(this Func<object, TEventArgs, Task> evt, object sender, TEventArgs eventArgs)
        {
            var handler = evt;
            if (handler == null)
                return;

            Delegate[] invocationList = handler.GetInvocationList();
            Task[] handlerTasks = new Task[invocationList.Length];
            for (var i = 0; i < invocationList.Length; i++)
                handlerTasks[i] = ((Func<object, TEventArgs, Task>)invocationList[i])(sender, eventArgs);

            await Task.WhenAll(handlerTasks);
        }
    }
}