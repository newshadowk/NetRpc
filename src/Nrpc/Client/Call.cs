using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nrpc
{
    internal sealed class Call : ICall
    {
        private readonly IConnectionFactory _factory;
        private readonly int _timeoutInterval;
        private readonly NrpcContext _context;

        public Call(IConnectionFactory factory, int timeoutInterval, NrpcContext context)
        {
            _factory = factory;
            _timeoutInterval = timeoutInterval;
            _context = context;
        }

        public async Task<T> CallAsync<T>(MethodInfoDto method, Action<object> callback, CancellationToken token, Stream stream, params object[] args)
        {
            var onceTransfer = _factory.Create();
            var t = new OnceCall<T>(onceTransfer, _timeoutInterval);
            t.Start();

            //header
            var header = _context.DefaultHeader.Clone();
            if (header.Count == 0)
                header = NrpcContext.ThreadHeader.Clone();
            NrpcContext.ThreadHeader.Clear();

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await t.CallAsync(header, method, callback, token, stream, args);
        }
    }
}