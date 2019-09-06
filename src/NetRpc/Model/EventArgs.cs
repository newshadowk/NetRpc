using System;

namespace NetRpc
{
    public sealed class ResultStreamEventArgs : EventArgs
    {
        public object Result { get; }

        public ResultStreamEventArgs(object result)
        {
            Result = result;
        }
    }
}