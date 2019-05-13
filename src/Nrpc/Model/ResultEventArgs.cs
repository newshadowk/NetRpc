using System;

namespace Nrpc
{
    internal sealed class ResultEventArgs : EventArgs
    {
        public bool HasStream { get; }
        
        public object Body { get; }

        public ResultEventArgs(bool hasStream, object body)
        {
            HasStream = hasStream;
            Body = body;
        }
    }
}