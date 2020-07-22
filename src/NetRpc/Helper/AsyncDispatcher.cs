using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    internal class InvokeFunc
    {
        public Func<object?> Func { get; }

        public WriteOnceBlock<(ExceptionDispatchInfo? exceptionDispatchInfo, object? result)> InvokedFlag { get; } =
            new WriteOnceBlock<(ExceptionDispatchInfo? exceptionDispatchInfo, object? result)>(null);

        public InvokeFunc(Func<object?> func)
        {
            Func = func;
        }
    }

    public class AsyncDispatcher : IDisposable
    {
        private readonly BufferBlock<InvokeFunc> _funcQ = new BufferBlock<InvokeFunc>();

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
                var action = await _funcQ.ReceiveAsync(_cts.Token);

                try
                {
                    var ret = action.Func.Invoke();
                    action.InvokedFlag.Post((null, ret));
                }
                catch (Exception e)
                {
                    action.InvokedFlag.Post((ExceptionDispatchInfo.Capture(e), null));
                }
            }
        }

        public void Invoke(Action action)
        {
            var invokeFunc = new InvokeFunc(() =>
            {
                action.Invoke();
                return null;
            });

            _funcQ.Post(invokeFunc);

            var (exceptionDispatchInfo, _) = invokeFunc.InvokedFlag.Receive();
            exceptionDispatchInfo?.Throw();
        }

        public async Task InvokeAsync(Action action)
        {
            var invokeFunc = new InvokeFunc(() =>
            {
                action.Invoke();
                return null;
            });

            _funcQ.Post(invokeFunc);
            var (exceptionDispatchInfo, _) = await invokeFunc.InvokedFlag.ReceiveAsync();
            exceptionDispatchInfo?.Throw();
        }

        public async Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
        {
            var invokeFunc = new InvokeFunc(() => callback.Invoke());
            _funcQ.Post(invokeFunc);
            var (exceptionDispatchInfo, result) = await invokeFunc.InvokedFlag.ReceiveAsync();
            exceptionDispatchInfo?.Throw();
            return (TResult)result!;
        }

        public Task BeginInvoke(Action action)
        {
            return InvokeAsync(action);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}