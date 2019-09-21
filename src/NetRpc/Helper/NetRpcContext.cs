using System.Collections.Generic;
using System.Threading;

namespace NetRpc
{
    public class NetRpcContext
    {
        private static readonly AsyncLocal<Dictionary<string, object>> _header = new AsyncLocal<Dictionary<string, object>>();

        public Dictionary<string, object> DefaultHeader { get; set; } = new Dictionary<string, object>();

        public static Dictionary<string, object> Header
        {
            get
            {
                if (_header.Value != null)
                    return _header.Value;

                _header.Value = new Dictionary<string, object>();
                return _header.Value;
            }
            set => _header.Value = value;
        }
    }
}