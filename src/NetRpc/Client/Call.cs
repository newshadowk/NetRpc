using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class Call : ICall
    {
        private readonly ContractInfo _contract;
        private readonly IOnceCallFactory _factory;
        private volatile int _timeoutInterval;
        private readonly ClientContext _context;

        public Call(ContractInfo contract, IOnceCallFactory factory, int timeoutInterval, ClientContext context)
        {
            _contract = contract;
            _factory = factory;
            _timeoutInterval = timeoutInterval;
            _context = context;
        }

        public void Config(int timeoutInterval)
        {
            _timeoutInterval = timeoutInterval;
        }

        public async Task<T> CallAsync<T>(MethodInfo methodInfo, Action<object> callback, CancellationToken token, Stream stream, params object[] args)
        {
            var call = _factory.Create<T>(_contract, _timeoutInterval);
            await call.StartAsync();

            //header
            var header = _context.DefaultHeader;
            if (header == null || header.Count == 0)
                header = ClientContext.Header;

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await call.CallAsync(header, methodInfo, callback, token, stream, args);
        }
    }
}