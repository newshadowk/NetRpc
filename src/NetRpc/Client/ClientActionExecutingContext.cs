using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class ClientActionExecutingContext : IActionExecutingContext
    {
        public Guid ClientProxyId { get; }

        public DateTimeOffset StartTime { get; }

        public IServiceProvider ServiceProvider { get; }

        public IOnceCall OnceCall { get; }

        /// <summary>
        /// Result of invoked action.
        /// </summary>
        public object? Result { get; set; }

        public Dictionary<string, object?> Headers { get; set; }

        /// <summary>
        /// A central location for sharing state between components during the invoking process.
        /// </summary>
        public Dictionary<object, object?> Properties { get; set; } = new();

        public ContractMethod ContractMethod { get; }

        public InstanceMethod InstanceMethod { get; }

        public ContractInfo Contract { get; }

        public Func<object?, Task>? Callback { get; }

        public CancellationToken CancellationToken { get; }

        public ProxyStream? Stream { get; }

        /// <summary>
        /// Args of invoked action without stream and action.
        /// </summary>
        public object?[] PureArgs { get; }

        public string? OptionsName { get; }

        public ClientActionExecutingContext(Guid clientProxyId,
            IServiceProvider serviceProvider,
            string? optionsName,
            IOnceCall onceCall,
            InstanceMethod instanceMethod,
            Func<object?, Task>? callback,
            CancellationToken token,
            ContractInfo contract,
            ContractMethod contractMethod,
            ProxyStream? stream,
            Dictionary<string, object?> header,
            object?[] pureArgs)
        {
            ClientProxyId = clientProxyId;
            StartTime = DateTimeOffset.Now;
            ServiceProvider = serviceProvider;
            OnceCall = onceCall;
            InstanceMethod = instanceMethod;
            Callback = callback;
            Contract = contract;
            ContractMethod = contractMethod;
            CancellationToken = token;
            Stream = stream;
            PureArgs = pureArgs;
            OptionsName = optionsName;
            Headers = header;
        }
    }
}