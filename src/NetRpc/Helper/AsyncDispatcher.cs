using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    public class AsyncDispatcher : IDisposable
    {
        private readonly BufferBlock<InvokeAction> _actionQ = new BufferBlock<InvokeAction>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public AsyncDispatcher()
        {
#pragma warning disable 4014
            StartAsync();
#pragma warning restore 4014
        }

        private async Task StartAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                var action = await _actionQ.ReceiveAsync(_cts.Token);
                action.Action.Invoke();
                action.InvokedFlag.Post(true);
            }
        }

        public void Invoke(Action action)
        {
            var invokeAction = new InvokeAction(action);
            _actionQ.Post(invokeAction);
            invokeAction.InvokedFlag.Receive();
        }

        public async Task InvokeAsync(Action action)
        {
            var invokeAction = new InvokeAction(action);
            _actionQ.Post(invokeAction);
            await invokeAction.InvokedFlag.ReceiveAsync();
        }

        public Task BeginInvoke(Action action)
        {
            return InvokeAsync(action);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        class InvokeAction
        {
            public Action Action { get; }

            public WriteOnceBlock<bool> InvokedFlag { get; } = new WriteOnceBlock<bool>(null);

            public InvokeAction(Action action)
            {
                Action = action;
            }
        }
    }
}