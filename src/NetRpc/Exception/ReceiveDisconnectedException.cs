using System;
using System.Runtime.Serialization;

namespace NetRpc
{
    [Serializable]
    public class ReceiveDisconnectedException : Exception
    {
        public ReceiveDisconnectedException(string message) : base(message)
        {
        }

        public ReceiveDisconnectedException()
        {
        }

        protected ReceiveDisconnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}