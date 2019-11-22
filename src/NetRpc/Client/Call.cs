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
        private readonly IServiceProvider _serviceProvider;
        private readonly ContractInfo _contract;
        private readonly IOnceCallFactory _factory;
        private volatile int _timeoutInterval;
        private readonly string _optionsName;
        private readonly ClientMiddlewareBuilder _middlewareBuilder;

        public Call(IServiceProvider serviceProvider, ContractInfo contract, IOnceCallFactory factory, int timeoutInterval, string optionsName)
        {
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

        public void Config(int timeoutInterval)
        {
            _timeoutInterval = timeoutInterval;
        }

        public Dictionary<string, object> AdditionHeader { get; set; } = new Dictionary<string, object>();

        public async Task<object> CallAsync(MethodInfo methodInfo, Action<object> callback, CancellationToken token, Stream stream, params object[] pureArgs)
        {
            var call = await _factory.CreateAsync(_timeoutInterval);
            await call.StartAsync();

            var contractMethod = _contract.Methods.Find(i => i.MethodInfo == methodInfo);
            var instanceMethod = new InstanceMethod(methodInfo);
            var methodContext = new MethodContext(contractMethod, instanceMethod);
            var clientContext = new ClientActionExecutingContext(_serviceProvider, _optionsName, call, instanceMethod, callback, token, _contract, contractMethod, stream, pureArgs);

            //header
            var header = AdditionHeader;
            if (header != null && header.Count > 0)
            {
                foreach (var key in header.Keys)
                    clientContext.Header.Add(key, header[key]);
            }

            if (_middlewareBuilder != null)
            {
                await _middlewareBuilder.InvokeAsync(clientContext);
                return clientContext.Result;
            }

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await call.CallAsync(header, methodContext, callback, token, stream, pureArgs);
        }
    }
}