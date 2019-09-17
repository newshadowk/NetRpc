using System;
using System.Runtime.Serialization;

namespace RabbitMQ.Base
{
    [Serializable]
    public sealed class MessageDeliverException : Exception
    {
        public MessageDeliverException()
        {
        }

        public MessageDeliverException(string message) : base(message)
        {
        }

        public MessageDeliverException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}