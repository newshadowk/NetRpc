using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class Call : ICall
    {
        private readonly Type _contactType;
        private readonly IOnceCallFactory _factory;
        private volatile int _timeoutInterval;
        private readonly NetRpcContext _context;

        public Call(Type contactType, IOnceCallFactory factory, int timeoutInterval, NetRpcContext context)
        {
            _contactType = contactType;
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
            var call = _factory.Create<T>(_contactType, _timeoutInterval);
            await call.StartAsync();

            //header
            var header = _context.DefaultHeader.Clone();
            if (header.Count == 0)
                header = NetRpcContext.ThreadHeader.Clone();
            NetRpcContext.ThreadHeader.Clear();

            //onceTransfer will dispose after stream translate finished in OnceCall.
            return await call.CallAsync(header, methodInfo, callback, token, stream, args);
        }
    }
}