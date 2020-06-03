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

        public Call(Guid clientProxyId, IServiceProvider serviceProvider, ContractInfo contract, IOnceCallFactory factory, int timeoutInterval,
            string optionsName)
        {
            _clientProxyId = clientProxyId;
            _serviceProvider = serviceProvider;
            _contract = contract;
            _factory = factory;
            _timeoutInterval = timeoutInterval;
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
            //cache header
            var header = AdditionHeader;

            //start
            var call = await _factory.CreateAsync(_timeoutInterval);
            await call.StartAsync(GetAuthorizationToken(header));

            //stream
            var contractMethod = _contract.Methods.Find(i => i.MethodInfo == methodInfo);
            var instanceMethod = new InstanceMethod(methodInfo);
            var methodContext = new MethodContext(contractMethod, instanceMethod);
            var readStream = GetReadStream(stream);
            var clientContext = new ClientActionExecutingContext(_clientProxyId, _serviceProvider, _optionsName, call, instanceMethod, callback, token,
                _contract,
                contractMethod, readStream, pureArgs);

            //header
            AddHeader(clientContext, header);

            //invoke
            if (_middlewareBuilder != null)
            {
                await _middlewareBuilder.InvokeAsync(clientContext);
                return clientContext.Result;
            }

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await call.CallAsync(header, methodContext, callback, token, stream, pureArgs);
        }

        public void Config(int timeoutInterval)
        {
            _timeoutInterval = timeoutInterval;
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

        private static void AddHeader(ClientActionExecutingContext context, Dictionary<string, object> additionHeader)
        {
            if (additionHeader != null && additionHeader.Count > 0)
                foreach (var key in additionHeader.Keys)
                    context.Header.Add(key, additionHeader[key]);
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