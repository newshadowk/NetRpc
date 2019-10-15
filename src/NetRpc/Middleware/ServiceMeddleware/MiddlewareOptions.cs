using System;
using System.Collections.Generic;

namespace NetRpc
{
    public class MiddlewareOptions
    {
        private readonly List<(Type Type, object[] args)> _items = new List<(Type Type, object[] args)>();

        private (Type Type, object[] args) _lastItem;

        public List<(Type Type, object[] args)> GetItems()
        {
            var ret = new List<(Type Type, object[] args)>();
            ret.AddRange(_items);
            if (_lastItem != default)
                ret.Add(_lastItem);
            return ret;
        }

        public void AddItems(List<(Type Type, object[] args)> items)
        {
            _items.AddRange(items);
        }

        public void UseMiddlewareLast(Type type, params object[] args)
        {
            if (_lastItem != default)
                throw new Exception();
            _lastItem = (type, args);
        }

        public void UseMiddleware<TMiddleware>(params object[] args)
        {
            UseMiddleware(typeof(TMiddleware), args);
        }

        public void UseMiddleware(Type type, params object[] args)
        {
            _items.Add((type, args));
        }

        public void UseCallbackThrottling(int callbackThrottlingInterval)
        {
            if (callbackThrottlingInterval <= 0)
                return;
            _items.Add((typeof(CallbackThrottlingMiddleware), new object[] {callbackThrottlingInterval}));
        }

        public void UseStreamCallBack(int progressCount)
        {
            if (progressCount <= 0)
                return;
            _items.Add((typeof(StreamCallBackMiddleware), new object[] { progressCount }));
        }
    }
}