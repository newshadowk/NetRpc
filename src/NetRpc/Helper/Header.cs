using System.Collections.Generic;

namespace NetRpc
{
    public class Header
    {
        private readonly Dictionary<string, object> _dic = new Dictionary<string, object>();

        public void CopyFrom(Dictionary<string, object> header)
        {
            _dic.Clear();
            foreach (var item in header)
                _dic.Add(item.Key, item.Value);
        }

        public Dictionary<string, object> Clone()
        {
            var ret = new Dictionary<string, object>();
            foreach (var item in _dic)
                ret.Add(item.Key, item.Value);
            return ret;
        }

        public void Clear()
        {
            _dic.Clear();
        }
    }
}