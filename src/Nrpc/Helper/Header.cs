using System.Collections.Generic;

namespace Nrpc
{
    public class Header
    {
        private readonly SyncDictionary<string, object> _dic = new SyncDictionary<string, object>();

        public void CopyFrom(Dictionary<string, object> header)
        {
            lock (_dic.SyncRoot)
            {
                _dic.Clear();
                foreach (var item in header)
                    _dic.Add(item.Key, item.Value);
            }
        }

        public Dictionary<string, object> Clone()
        {
            var ret = new Dictionary<string, object>();
            lock (_dic.SyncRoot)
            {
                foreach (var item in _dic)
                    ret.Add(item.Key, item.Value);
            }
            return ret;
        }

        public void Clear()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _dic.Clear();
        }
    }
}