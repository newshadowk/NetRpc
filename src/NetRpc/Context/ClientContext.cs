using System.Collections.Generic;
using System.Threading;

namespace NetRpc
{
    public sealed class ClientContext
    {
        private static readonly AsyncLocal<Dictionary<string, object>> _local = new AsyncLocal<Dictionary<string, object>>();

        public Dictionary<string, object> DefaultHeader { get; set; } = new Dictionary<string, object>();

        public static Dictionary<string, object> Header
        {
            get
            {
                if (_local.Value != null)
                    return _local.Value;

                _local.Value = new Dictionary<string, object>();
                return _local.Value;
            }
            set => _local.Value = value;
        }
    }
}