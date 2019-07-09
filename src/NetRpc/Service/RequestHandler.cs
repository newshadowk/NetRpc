using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class RequestHandler
    {
        public object[] Instances { get; }

        private readonly MiddlewareRegister _middlewareRegister;

        public RequestHandler(MiddlewareRegister middlewareRegister, params object[] instances)
        {
            Instances = instances;
            _middlewareRegister = middlewareRegister;
        }

        public async Task HandleAsync(IConnection connection)
        {
            await HandleAsync(new BufferServiceOnceApiConvert(connection));
        }

        public async Task HandleAsync(IBufferServiceOnceApiConvert convert)
        {
            var t = new BufferServiceOnceTransfer(convert, _middlewareRegister, Instances);
            t.Start();
            await t.HandleRequestAsync();
        }

        public async Task HandleAsync(IHttpServiceOnceApiConvert convert)
        {
            var t = new HttpServiceOnceTransfer(convert, _middlewareRegister, Instances);
            t.Start();
            await t.HandleRequestAsync();
        }
    }
}