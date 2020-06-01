using System;
using System.IO;

namespace NetRpc
{
    public abstract class ReadStream : Stream, IProgress
    {
        public event EventHandler<SizeEventArgs> Finished;
        public event EventHandler Started;
        public event EventHandler<SizeEventArgs> Progress;
        private volatile bool _isStarted;
        private volatile bool _isFinished;

        public Stream CacheStream { get; set; }

        protected void WriteCache(byte[] buffer, int count)
        {
            CacheStream.Write(buffer, 0, count);
        }

        protected void InvokeFinish(SizeEventArgs e)
        {
            if (!_isStarted)
                return;

            if (!_isFinished)
            {
                _isFinished = true;
                OnFinished(e);
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

        protected void OnStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnFinished(SizeEventArgs e)
        {
            Finished?.Invoke(this, e);
        }

        protected virtual void OnProgress(SizeEventArgs e)
        {
            Progress?.Invoke(this, e);
        }
    }
}