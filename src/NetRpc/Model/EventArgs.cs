using System;

namespace NetRpc
{
    internal sealed class ResultStreamEventArgs : EventArgs
    {
        public long? StreamLength { get; }

        public ResultStreamEventArgs(long? streamLength)
        {
            StreamLength = streamLength;
        }
    }
}