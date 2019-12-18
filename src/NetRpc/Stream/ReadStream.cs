using System;
using System.IO;

namespace NetRpc
{
    public abstract class ReadStream : Stream, IProgress
    {
        public event EventHandler Finished;
        public event EventHandler Started;
        public event EventHandler<long> Progress;
        private volatile bool _isStarted;
        private volatile bool _isFinished;

        protected void InvokeFinish()
        {
            if (!_isFinished)
            {
                _isFinished = true;
                OnFinished();
            }
        }

        protected void InvokeStart()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                OnStarted();
            }
        }

        protected void OnFinished()
        {
            Finished?.Invoke(this, EventArgs.Empty);
        }

        protected void OnProgress(long e)
        {
            Progress?.Invoke(this, e);
        }

        protected void OnStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }
    }
}