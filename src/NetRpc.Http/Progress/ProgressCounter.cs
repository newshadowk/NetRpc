using System;
using System.Collections.Generic;
using System.Timers;

namespace NetRpc.Http
{
    internal sealed class ProgressCounter : IDisposable
    {
        private readonly Timer _tSpeed = new Timer(1000);
        private long _currSize;
        private readonly object _lockObj = new object();
        private readonly long _totalSize;
        private int _speed;
        private TimeSpan _leftTimeSpan;
        private readonly Queue<long> _qOldSize = new Queue<long>();
        private const int GapSecs = 2;

        public int Speed
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

        private static (int speed, TimeSpan leftTimeSpan) Count(long currSize, long totalSize, long oldSize, int spanSecs)
        {
            var speed = (int)(currSize - oldSize) / spanSecs;
            long leftSec;
            if (speed == 0)
                leftSec = (long)TimeSpan.MaxValue.TotalSeconds;
            else
                leftSec = (totalSize - currSize) / speed;
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
            _tSpeed?.Dispose();
        }
    }
}