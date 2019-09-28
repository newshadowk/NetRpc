using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NetRpc
{
    //public sealed class ClientContext
    //{
    //    private static readonly AsyncLocal<Dictionary<string, object>> _local = new AsyncLocal<Dictionary<string, object>>();

    //    public Dictionary<string, object> DefaultHeader { get; set; } = new Dictionary<string, object>();

    //    public static Dictionary<string, object> Header
    //    {
    //        get
    //        {
    //            if (_local.Value != null)
    //                return _local.Value;

    //            _local.Value = new Dictionary<string, object>();
    //            return _local.Value;
    //        }
    //        set => _local.Value = value;
    //    }
    //}

    public sealed class ClientContext
    {
        public IServiceProvider ServiceProvider { get; }

        internal IOnceCall OnceCall { get; }

        public object Result { get; set; }

        public Dictionary<string, object> Header { get; set; } = new Dictionary<string, object>();

        public MethodInfo MethodInfo { get; }

        public Action<object> Callback { get; }

        public CancellationToken Token { get; }

        public Stream Stream { get; }

        public object[] Args { get; }

        public ClientContext(IServiceProvider serviceProvider, 
            IOnceCall onceCall, 
            MethodInfo methodInfo, 
            Action<object> callback, 
            CancellationToken token, 
            Stream stream, 
            object[] args)
        {
            ServiceProvider = serviceProvider;
            OnceCall = onceCall;
            MethodInfo = methodInfo;
            Callback = callback;
            Token = token;
            Stream = stream;
            Args = args;
        }
    }
}