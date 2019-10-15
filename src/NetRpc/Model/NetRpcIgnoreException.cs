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

        protected NetRpcIgnoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}