using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class Call : ICall
    {
        private readonly Guid _clientProxyId;
        private readonly ContractInfo _contract;
        private readonly IOnceCallFactory _factory;
        private readonly ClientMiddlewareBuilder _middlewareBuilder;
        private readonly string? _optionsName;
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
        private volatile int _timeoutInterval;
        private readonly bool _forwardHeader;
        private static readonly AsyncLocal<Dictionary<string, object?>> AsyncLocalHeader = new();

        public Call(Guid clientProxyId,
            ContractInfo contractInfo,
            IServiceProvider serviceProvider,
            ClientMiddlewareOptions middlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IOnceCallFactory factory,
            int timeoutInterval,
            bool forwardHeader,
            string? optionsName)
        {
            _clientProxyId = clientProxyId;
            _serviceProvider = serviceProvider;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _contract = contractInfo;
            _factory = factory;
            _timeoutInterval = timeoutInterval;
            _forwardHeader = forwardHeader;
            _optionsName = optionsName;
            _middlewareBuilder = new ClientMiddlewareBuilder(middlewareOptions, serviceProvider);
        }

        public Dictionary<string, object?> AdditionHeader { get; set; } = new();

        public static Dictionary<string, object?> AdditionContextHeader
        {
            get
            {
                if (AsyncLocalHeader.Value == null)
                    AsyncLocalHeader.Value = new Dictionary<string, object?>();
                return AsyncLocalHeader.Value;
            }
            set => AsyncLocalHeader.Value = value;
        }

        public async Task<object?> CallAsync(MethodInfo methodInfo, Func<object?, Task>? callback, CancellationToken token, Stream? stream,
            params object?[] pureArgs)
        {
            //merge header
            var mergeHeader = MergeHeader();

            //start
            var call = await _factory.CreateAsync(_timeoutInterval);
            await call.StartAsync(GetAuthorizationToken(mergeHeader));

            //stream
            var contractMethod = _contract.Methods.First(i => i.MethodInfo == methodInfo);
            var instanceMethod = new InstanceMethod(methodInfo);
            var readStream = GetReadStream(stream);
            var clientContext = new ClientActionExecutingContext(_clientProxyId, _serviceProvider, _optionsName, call, instanceMethod, callback, token,
                _contract, contractMethod, readStream, mergeHeader, pureArgs);

            //invoke
            //onceTransfer will dispose after stream translate finished in OnceCall.
            await _middlewareBuilder.InvokeAsync(clientContext);
            return clientContext.Result!;
        }

        private static ReadStream? GetReadStream(Stream? stream)
        {
            ReadStream? readStream;
            switch (stream)
            {
                case null:
                    readStream = null;
                    break;
                case ReadStream rs:
                    readStream = rs;
                    break;
                default:
                    readStream = new ProxyStream(stream);
                    break;
            }

            return readStream;
        }

        private Dictionary<string, object?> MergeHeader()
        {
            //_actionExecutingContextAccessor.Context?.Header is immutable here, should only change via middleware.
            var contextH = _actionExecutingContextAccessor.Context?.Header;
            var dic = new Dictionary<string, object?>();
            if (_forwardHeader)
            {
                if (contextH != null)
                    foreach (var key in contextH.Keys)
                        dic.Add(key, contextH[key]);
            }

            foreach (var key in AdditionHeader.Keys)
                dic[key] = AdditionHeader[key];

            foreach (var key in AdditionContextHeader.Keys)
                dic[key] = AdditionContextHeader[key];

            return dic;
        }

        private static string? GetAuthorizationToken(Dictionary<string, object?> additionHeader)
        {
            if (!additionHeader.TryGetValue("Authorization", out var v))
                return null;

            var s = v!.ToString();
            if (s == null || !s.StartsWith("Bearer "))
                return null;

            return s.Substring(7);
        }
    }
}