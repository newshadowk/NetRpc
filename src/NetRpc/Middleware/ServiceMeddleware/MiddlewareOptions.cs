using System;
using System.Collections.Generic;

namespace NetRpc
{
    public class MiddlewareOptions
    {
        public List<(Type Type, object[] args)> Items { get; set; } = new List<(Type Type, object[] args)>();

        public void UseMiddleware<TMiddleware>(params object[] args)
        {
            UseMiddleware(typeof(TMiddleware), args);
        }

        public void UseMiddleware(Type type, params object[] args)
        {
            Items.Add((type, args));
        }

        public void UseCallbackThrottling(int callbackThrottlingInterval)
        {
            if (callbackThrottlingInterval <= 0)
                return;
            Items.Add((typeof(CallbackThrottlingMiddleware), new object[] {callbackThrottlingInterval}));
        }

        public void UseStreamCallBack(int progressCount)
        {
            if (progressCount <= 0)
                return;
            Items.Add((typeof(StreamCallBackMiddleware), new object[] { progressCount }));
        }
    }
}