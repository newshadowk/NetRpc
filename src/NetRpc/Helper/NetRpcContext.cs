using System;

namespace NetRpc
{
    public class NetRpcContext
    {
        [ThreadStatic] private static Header _header;

        private static readonly object LockObj = new object();

        private static Header GetHeader()
        {
            if (_header == null)
            {
                lock (LockObj)
                {
                    if (_header == null)
                        _header = new Header();
                }
            }

            return _header;
        }

        public Header DefaultHeader { get; } = new Header();

        public static Header ThreadHeader => GetHeader();
    }
}