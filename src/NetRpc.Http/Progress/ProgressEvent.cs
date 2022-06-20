using System;

namespace NetRpc.Http;

internal sealed class ProgressEvent : IDisposable
{
    private readonly object _lockObj = new();

    private ProgressCounter? _speedCounter;

    public ProgressEventArgs DownLoaderProgress(long currSize, long totalSize)
    {
        lock (_lockObj)
        {
            _speedCounter ??= new ProgressCounter(currSize, totalSize);
            _speedCounter.Update(currSize);

            int percent;

            if (totalSize == 0)
                percent = 0;
            else
                percent = (int) ((double) currSize / totalSize * 100);

            return new ProgressEventArgs(currSize, totalSize, percent, (long) _speedCounter.LeftTime.TotalSeconds, _speedCounter.Speed,
                NetRpc.Helper.SizeSuffix(_speedCounter.Speed));
        }
    }

    public void Dispose()
    {
        _speedCounter?.Dispose();
    }
}