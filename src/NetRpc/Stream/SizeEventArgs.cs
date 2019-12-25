using System;

namespace NetRpc
{
    public class SizeEventArgs : EventArgs
    {
        public long Value { get; }

        public SizeEventArgs(long value)
        {
            Value = value;
        }
    }
}