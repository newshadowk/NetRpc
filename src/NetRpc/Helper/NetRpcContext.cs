using System;

namespace NetRpc
{
    public class NetRpcContext
    {
        [ThreadStatic] private static Header _header;

        public Header DefaultHeader { get; } = new Header();

        public static Header ThreadHeader => _header ?? (_header = new Header());
    }
}