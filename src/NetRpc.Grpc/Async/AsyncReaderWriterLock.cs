using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lib.Base
{
    public class AsyncReaderWriterLock
    {
        private readonly Task<Release> _readerRelease;
        private readonly Task<Release> _writerRelease;

        private readonly Queue<TaskCompletionSource<Release>> _waitingWriters =
            new Queue<TaskCompletionSource<Release>>();

        private TaskCompletionSource<Release> _waitingReader =
            new TaskCompletionSource<Release>();

        private int _readersWaiting;
        private int _status;

        public AsyncReaderWriterLock()
        {
            _readerRelease = Task.FromResult(new Release(this, false));
            _writerRelease = Task.FromResult(new Release(this, true));
        }

        public Task<Release> ReaderLockAsync()
        {
            lock (_waitingWriters)
            {
                if (_status >= 0 && _waitingWriters.Count == 0)
                {
                    ++_status;
                    return _readerRelease;
                }

                ++_readersWaiting;
                return _waitingReader.Task.ContinueWith(t => t.Result);
            }
        }

        private void ReaderRelease()
        {
            TaskCompletionSource<Release> toWake = null;

            lock (_waitingWriters)
            {
                --_status;
                if (_status == 0 && _waitingWriters.Count > 0)
                {
                    _status = -1;
                    toWake = _waitingWriters.Dequeue();
                }
            }

            toWake?.SetResult(new Release(this, true));
        }

        public Task<Release> WriterLockAsync()
        {
            lock (_waitingWriters)
            {
                if (_status == 0)
                {
                    _status = -1;
                    return _writerRelease;
                }

                var waiter = new TaskCompletionSource<Release>();
                _waitingWriters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        private void WriterRelease()
        {
            TaskCompletionSource<Release> toWake = null;
            bool toWakeIsWriter = false;

            lock (_waitingWriters)
            {
                if (_waitingWriters.Count > 0)
                {
                    toWake = _waitingWriters.Dequeue();
                    toWakeIsWriter = true;
                }
                else if (_readersWaiting > 0)
                {
                    toWake = _waitingReader;
                    _status = _readersWaiting;
                    _readersWaiting = 0;
                    _waitingReader = new TaskCompletionSource<Release>();
                }
                else _status = 0;
            }

            toWake?.SetResult(new Release(this, toWakeIsWriter));
        }

        public struct Release : IDisposable
        {
            private readonly AsyncReaderWriterLock m_toRelease;
            private readonly bool m_writer;

            internal Release(AsyncReaderWriterLock toRelease, bool writer)
            {
                m_toRelease = toRelease;
                m_writer = writer;
            }

            public void Dispose()
            {
                if (m_toRelease != null)
                {
                    if (m_writer)
                        m_toRelease.WriterRelease();
                    else
                        m_toRelease.ReaderRelease();
                }
            }
        }
    }
}