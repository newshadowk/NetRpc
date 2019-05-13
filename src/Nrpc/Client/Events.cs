using System;

namespace Nrpc
{
    public sealed class EventArgsT<T> : EventArgs
    {
        public EventArgsT(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}