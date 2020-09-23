namespace NetRpc.Http
{
    public sealed class ProgressEventArgs
    {
        public long CurrSize { get; set; }

        public long TotalSize { get; set; }

        public double Percent { get; set; }

        public long LeftSeconds { get; set; }

        /// <summary>
        /// byte/s
        /// </summary>
        public long Speed { get; set; }

        public string SpeedStr { get; set; }

        public ProgressEventArgs(long currSize, long totalSize, double percent, long leftSeconds, long speed, string speedStr)
        {
            CurrSize = currSize;
            TotalSize = totalSize;
            Percent = percent;
            LeftSeconds = leftSeconds;
            Speed = speed;
            SpeedStr = speedStr;
        }

        public override string ToString()
        {
            return $"{nameof(CurrSize)}: {CurrSize}, {nameof(TotalSize)}: {TotalSize}, {nameof(Percent)}: {Percent}, {nameof(LeftSeconds)}: {LeftSeconds}, {nameof(Speed)}: {Speed}, {nameof(SpeedStr)}: {SpeedStr}";
        }
    }
}