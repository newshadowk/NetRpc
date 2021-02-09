using System;
using System.Runtime.Serialization;

namespace NetRpc
{
    [Serializable]
    public class DisconnectedException : Exception
    {
        public DisconnectedException(string message) : base(message)
        {
        }

        public DisconnectedException()
        {
        }

        protected DisconnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        public DisconnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}