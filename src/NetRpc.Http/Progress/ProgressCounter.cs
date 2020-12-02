using System;
using System.Collections.Generic;
using System.Timers;

namespace NetRpc.Http
{
    internal sealed class ProgressCounter : IDisposable
    {
        private readonly Timer _tSpeed = new(1000);
        private long _currSize;
        private readonly object _lockObj = new();
        private readonly long _totalSize;
        private long _speed;
        private TimeSpan _leftTimeSpan;
        private readonly Queue<long> _qOldSize = new();
        private const int GapSecs = 2;

        public long Speed
        {
            get
            {
                lock (_lockObj)
                {
                    return _speed;
                }
            }
        }

        public TimeSpan LeftTime
        {
            get
            {
                lock (_lockObj)
                {
                    return _leftTimeSpan;
                }
            }
        }

        public ProgressCounter(long currSize, long totalSize)
        {
            _totalSize = totalSize;
            _qOldSize.Enqueue(currSize);
            _tSpeed.Start();
            _tSpeed.Elapsed += TSpeedElapsed;
        }

        private void TSpeedElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lockObj)
            {
                if (_qOldSize.Count == 0)
                {
                    _qOldSize.Enqueue(_currSize);
                    return;
                }

                var (spanSecs, oldSize) = GetDataFromQueue(_qOldSize);
                var (speed, leftTimeSpan) = Count(_currSize, _totalSize, oldSize, spanSecs);
                _speed = speed;
                _leftTimeSpan = leftTimeSpan;
                _qOldSize.Enqueue(_currSize);
            }
        }

        private static (int spanSecs, long oldSize) GetDataFromQueue(Queue<long> q)
        {
            if (q.Count < GapSecs)
                return (q.Count, q.Peek());

            if (q.Count == GapSecs)
                return (GapSecs, q.Dequeue());

            throw new ArgumentOutOfRangeException("", $"ProgessCounter.GetDataFromQueue() failed. q.Count is greater than {GapSecs} ");
        }

        private static (long speed, TimeSpan leftTimeSpan) Count(long currSize, long totalSize, long oldSize, int spanSecs)
        {
            var speed = (currSize - oldSize) / spanSecs;

            long leftSec;
            if (speed == 0 || totalSize == 0)
                leftSec = 0;
            else
            {
                leftSec = (totalSize - currSize) / speed;
                if (leftSec >= TimeSpan.MaxValue.TotalSeconds)
                    leftSec = 0;
            }

            var leftTimeSpan = TimeSpan.FromSeconds(leftSec);

            return (speed, leftTimeSpan);
        }

        public void Update(long currSize)
        {
            lock (_lockObj)
            {
                _currSize = currSize;
            }
        }

        public void Dispose()
        {
            _tSpeed.Dispose();
        }
    }
}