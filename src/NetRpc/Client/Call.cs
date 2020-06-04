using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NetRpc
{
    internal sealed class Call : ICall
    {
        private readonly Guid _clientProxyId;
        private readonly ContractInfo _contract;
        private readonly IOnceCallFactory _factory;
        private readonly ClientMiddlewareBuilder _middlewareBuilder;
        private readonly string _optionsName;
        private readonly IServiceProvider _serviceProvider;
        private volatile int _timeoutInterval;
        private readonly bool _forwardHeader;

        public Call(Guid clientProxyId, IServiceProvider serviceProvider, ContractInfo contract, IOnceCallFactory factory, int timeoutInterval, bool forwardHeader,
            string optionsName)
        {
            _clientProxyId = clientProxyId;
            _serviceProvider = serviceProvider;
            _contract = contract;
            _factory = factory;
            _timeoutInterval = timeoutInterval;
            _forwardHeader = forwardHeader;
            _optionsName = optionsName;

            if (serviceProvider != null)
            {
                var middlewareOptions = _serviceProvider.GetService<IOptions<ClientMiddlewareOptions>>().Value;
                _middlewareBuilder = new ClientMiddlewareBuilder(middlewareOptions, serviceProvider);
            }
        }

        public Dictionary<string, object> AdditionHeader { get; set; } = new Dictionary<string, object>();

        public async Task<object> CallAsync(MethodInfo methodInfo, Func<object, Task> callback, CancellationToken token, Stream stream,
            params object[] pureArgs)
        {
            //merge header
            var mergeHeader = MergeHeader(AdditionHeader);

            //start
            var call = await _factory.CreateAsync(_timeoutInterval);
            await call.StartAsync(GetAuthorizationToken(mergeHeader));

            //stream
            var contractMethod = _contract.Methods.Find(i => i.MethodInfo == methodInfo);
            var instanceMethod = new InstanceMethod(methodInfo);
            var methodContext = new MethodContext(contractMethod, instanceMethod);
            var readStream = GetReadStream(stream);
            var clientContext = new ClientActionExecutingContext(_clientProxyId, _serviceProvider, _optionsName, call, instanceMethod, callback, token,
                _contract, contractMethod, readStream, mergeHeader, pureArgs);

            //invoke
            if (_middlewareBuilder != null)
            {
                await _middlewareBuilder.InvokeAsync(clientContext);
                return clientContext.Result;
            }

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await call.CallAsync(mergeHeader, methodContext, callback, token, stream, pureArgs);
        }

        private static ReadStream GetReadStream(Stream stream)
        {
            ReadStream readStream;
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

        private Dictionary<string, object> MergeHeader(Dictionary<string, object> additionHeader)
        {
            var dic = new Dictionary<string, object>();
           
            if (_forwardHeader)
            {
                var contextHeader = GlobalActionExecutingContext.Context.Header;
                if (contextHeader != null && contextHeader.Count > 0)
                    foreach (var key in contextHeader.Keys)
                        dic.Add(key, contextHeader[key]);
            }

            if (additionHeader != null && additionHeader.Count > 0)
                foreach (var key in additionHeader.Keys)
                    dic[key] = additionHeader[key];

            return dic;
        }

        private static string GetAuthorizationToken(Dictionary<string, object> additionHeader)
        {
            if (additionHeader == null)
                return null;

            if (!additionHeader.TryGetValue("Authorization", out var v))
                return null;

            var s = v.ToString();
            if (s == null || !s.StartsWith("Bearer "))
                return null;

            return s.Substring(7);
        }
    }
}