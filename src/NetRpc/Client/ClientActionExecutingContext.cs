using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NetRpc
{
    public sealed class ClientActionExecutingContext : IActionExecutingContext
    {
        public IServiceProvider ServiceProvider { get; }

        public IOnceCall OnceCall { get; }

        /// <summary>
        /// Result of invoked action.
        /// </summary>
        public object Result { get; set; }

        public Dictionary<string, object> Header { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// A central location for sharing state between components during the invoking process.
        /// </summary>
        public Dictionary<object, object> Properties { get; set; } = new Dictionary<object, object>();

        public ContractMethod ContractMethod { get; set; }

        public InstanceMethod InstanceMethod { get; set; }

        public ContractInfo ContractInfo { get; }

        public Action<object> Callback { get; }

        public CancellationToken CancellationToken { get; }

        public ReadStream Stream { get; }

        /// <summary>
        /// Args of invoked action without stream and action.
        /// </summary>
        public object[] PureArgs { get; }

        public string OptionsName { get; }

        public ClientActionExecutingContext(IServiceProvider serviceProvider, 
            string optionsName,
            IOnceCall onceCall,
            InstanceMethod instanceMethod, 
            Action<object> callback, 
            CancellationToken token,
            ContractInfo contractInfo,
            ContractMethod contractMethod,
            ReadStream stream, 
            object[] pureArgs)
        {
            ServiceProvider = serviceProvider;
            OnceCall = onceCall;
            InstanceMethod = instanceMethod;
            Callback = callback;
            ContractInfo = contractInfo;
            ContractMethod = contractMethod;
            CancellationToken = token;
            Stream = stream;
            PureArgs = pureArgs;
            OptionsName = optionsName;
        }
    }
}