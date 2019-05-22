using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class RequestHandler
    {
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;

        public RequestHandler(MiddlewareRegister middlewareRegister, object[] instances)
        {
            _instances = instances;
            _middlewareRegister = middlewareRegister;
        }

        public async Task HandleAsync(IConnection connection)
        {
            var t = new ServiceTransfer(connection, _middlewareRegister, _instances);
            t.Start();
            await t.HandleRequestAsync();
        }
    }
}