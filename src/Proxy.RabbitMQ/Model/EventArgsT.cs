using System;

namespace RabbitMQ.Base
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