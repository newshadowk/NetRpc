using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class Call : ICall
    {
        private readonly IConnectionFactory _factory;
        private volatile bool _isWrapFaultException;
        private volatile int _timeoutInterval;
        private readonly NetRpcContext _context;

        public Call(IConnectionFactory factory, bool isWrapFaultException, int timeoutInterval, NetRpcContext context)
        {
            _factory = factory;
            _isWrapFaultException = isWrapFaultException;
            _timeoutInterval = timeoutInterval;
            _context = context;
        }

        public void Config(bool isWrapFaultException, int timeoutInterval)
        {
            _isWrapFaultException = isWrapFaultException;
            _timeoutInterval = timeoutInterval;
        }

        public async Task<T> CallAsync<T>(ActionInfo action, Action<object> callback, CancellationToken token, Stream stream, params object[] args)
        {
            var onceTransfer = _factory.Create();
            var t = new OnceCall<T>(onceTransfer, _isWrapFaultException, _timeoutInterval);
            t.Start();

            //header
            var header = _context.DefaultHeader.Clone();
            if (header.Count == 0)
                header = NetRpcContext.ThreadHeader.Clone();
            NetRpcContext.ThreadHeader.Clear();

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await t.CallAsync(header, action, callback, token, stream, args);
        }
    }
}