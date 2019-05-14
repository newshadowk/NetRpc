using System.Threading.Tasks;

namespace NetRpc
{
    public static class NetRpc
    {
        public static async Task HandleRequestAsync(IConnection connection, object[] instances, MiddlewareRegister middlewareRegister)
        {
            var t = new ServiceTransfer(connection, instances, middlewareRegister);
            t.Start();
            await t.HandleRequestAsync();
        }
    }
}