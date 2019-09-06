using System;
using System.Runtime.Serialization;

namespace NetRpc
{
    [Serializable]
    public class MethodNotFoundException : Exception
    {
        public MethodNotFoundException()
        {
        }

        protected MethodNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MethodNotFoundException(string message) : base(message)
        {
        }
    }
}