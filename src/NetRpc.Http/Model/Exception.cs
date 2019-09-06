using System;

namespace NetRpc.Http
{
    internal class HttpNotMatchedException : Exception
    {
        public HttpNotMatchedException(string message) : base(message)
        {
        }
    }

    internal class HttpFailedException : Exception
    {
        public HttpFailedException(string message) : base(message)
        {
        }
    }
}