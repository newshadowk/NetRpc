using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc.Http.Client
{
    public sealed class SyncCanceler : IDisposable
    {
        private volatile bool _isEnd;

        public bool IsTimeoutCancel;

        private readonly object _lockObj = new object();

        public SyncCanceler(Action cancelAction, CancellationToken token, int timeout)
        {
            Task.Run(async () =>
            {
                var starTime = DateTime.Now;
                while (true)
                {
                    if (_isEnd)
                    {
                        return;
                    }

                    if (token.IsCancellationRequested)
                    {
                        SafeInvoke(cancelAction);
                        return;
                    }

                    try
                    {
                        await Task.Delay(500);
                    }
                    catch (TaskCanceledException)
                    {
                        SafeInvoke(cancelAction);
                        return;
                    }

                    var endTime = DateTime.Now;
                    var ts = endTime - starTime;
                    if (ts.TotalMilliseconds > timeout)
                    {
                        IsTimeoutCancel = true;
                        SafeInvoke(cancelAction);
                        return;
                    }
                }
            });
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                lock (_lockObj)
                {
                    if (!_isEnd)
                    {
                        action.Invoke();
                    }
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                _isEnd = true;
            }
        }
    }
}