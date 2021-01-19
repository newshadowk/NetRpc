using System;
using System.IO;
using System.Threading.Tasks;

namespace NetRpc
{
    public abstract class EventStream : Stream
    {
        protected volatile bool IsStarted;
        protected volatile bool IsFinished;
        public event AsyncEventHandler<SizeEventArgs>? FinishedAsync;
        public event AsyncEventHandler? StartedAsync;
        public event AsyncEventHandler<SizeEventArgs>? ProgressAsync;

        public virtual void Reset()
        {
            IsStarted = false;
            IsFinished = false;
        }

        protected async Task InvokeFinishAsync(SizeEventArgs e)
        {
            if (!IsStarted)
                return;

            if (!IsFinished)
            {
                IsFinished = true;
                await OnFinishedAsync(e);
            }
        }

        protected async Task InvokeStartAsync()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                await OnStartedAsync();
            }
        }

        protected virtual Task OnStartedAsync()
        {
            return StartedAsync.InvokeAsync(this, EventArgs.Empty);
        }

        protected virtual Task OnFinishedAsync(SizeEventArgs e)
        {
            return FinishedAsync.InvokeAsync(this, e);
        }

        protected virtual Task OnProgressAsync(SizeEventArgs e)
        {
            return ProgressAsync.InvokeAsync(this, e);
        }
    }
}