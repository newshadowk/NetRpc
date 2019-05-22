using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class RequestHandler
    {
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;
        private readonly bool _isWrapFaultException;

        public RequestHandler(MiddlewareRegister middlewareRegister, bool isWrapFaultException, object[] instances)
        {
            _instances = instances;
            _middlewareRegister = middlewareRegister;
            _isWrapFaultException = isWrapFaultException;
        }

        public async Task HandleAsync(IConnection connection)
        {
            var t = new ServiceTransfer(connection, _middlewareRegister, _isWrapFaultException, _instances);
            t.Start();
            await t.HandleRequestAsync();
        }
    }
}