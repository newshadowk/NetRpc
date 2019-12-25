using System;

namespace NetRpc.Http
{
    internal sealed class ProgressEvent : IDisposable
    {
        private readonly object _lockObj = new object();

        private ProgressCounter _speedCounter;

        public ProgressEventArgs DownLoaderProgress(long currSize, long totalSize)
        {
            lock (_lockObj)
            {
                if (_speedCounter == null)
                    _speedCounter = new ProgressCounter(currSize, totalSize);

                _speedCounter.Update(currSize);

                int percent;
                if (totalSize == 0 && currSize == 0)
                    percent = 100;
                else
                    percent = (int)((double)currSize / totalSize * 100);

                return new ProgressEventArgs(currSize, totalSize, percent, (int)_speedCounter.LeftTime.TotalSeconds, _speedCounter.Speed,
                    NetRpc.Helper.SizeSuffix(_speedCounter.Speed));
            }
        }

        public void Dispose()
        {
            _speedCounter?.Dispose();
        }
    }
}