using System;
using System.Runtime.Serialization;

namespace NetRpc
{
    [Serializable]
    public class NIgnoreException : Exception
    {
        public NIgnoreException()
        {
        }

        public NIgnoreException(string message) : base(message)
        {
        }

        protected NIgnoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}