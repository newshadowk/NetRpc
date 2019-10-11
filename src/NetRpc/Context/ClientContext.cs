using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NetRpc
{
    public sealed class ClientContext
    {
        public IServiceProvider ServiceProvider { get; }

        internal IOnceCall OnceCall { get; }

        /// <summary>
        /// Result of invoked action.
        /// </summary>
        public object Result { get; set; }

        public Dictionary<string, object> Header { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// A central location for sharing state between components during the invoking process.
        /// </summary>
        public Dictionary<object, object> Properties { get; set; } = new Dictionary<object, object>();

        public MethodInfo MethodInfo { get; }

        public Action<object> Callback { get; }

        public CancellationToken Token { get; }

        public ContractInfo ContractInfo { get; }

        public MethodObj MethodObj { get; }

        public Stream Stream { get; }

        /// <summary>
        /// Args of invoked action without stream and action.
        /// </summary>
        public object[] PureArgs { get; }

        public string OptionsName { get; }

        public ClientContext(IServiceProvider serviceProvider, 
            string optionsName,
            IOnceCall onceCall, 
            MethodInfo methodInfo, 
            Action<object> callback, 
            CancellationToken token,
            ContractInfo contractInfo,
            MethodObj methodObj,
            Stream stream, 
            object[] pureArgs)
        {
            ServiceProvider = serviceProvider;
            OnceCall = onceCall;
            MethodInfo = methodInfo;
            Callback = callback;
            ContractInfo = contractInfo;
            MethodObj = methodObj;
            Token = token;
            Stream = stream;
            PureArgs = pureArgs;
            OptionsName = optionsName;
        }
    }
}