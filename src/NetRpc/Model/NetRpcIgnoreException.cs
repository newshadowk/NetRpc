using System;
using System.Runtime.Serialization;

namespace NetRpc
{
    [Serializable]
    public class NetRpcIgnoreException : Exception
    {
        public NetRpcIgnoreException()
        {
        }

        public NetRpcIgnoreException(string message) : base(message)
        {
        }

        protected NetRpcIgnoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}