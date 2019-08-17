using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class HttpServiceOnceTransfer : ServiceOnceTransferBase
    {
        private readonly IHttpServiceOnceApiConvert _convert;
        private readonly object[] _instances;
        private readonly IServiceProvider _serviceProvider;
        private readonly MiddlewareBuilder _middlewareBuilder;

        public HttpServiceOnceTransfer(object[] instances, IServiceProvider serviceProvider, IHttpServiceOnceApiConvert convert, MiddlewareBuilder middlewareBuilder) 
            : base(convert)
        {
            _convert = convert;
            _instances = instances;
            _serviceProvider = serviceProvider;
            _middlewareBuilder = middlewareBuilder;
        }

        public override async Task HandleRequestAsync()
        {
            object ret;
            RpcContext rpcContext = null;

            try
            {
                var scp = await GetServiceCallParamAsync();
                rpcContext = ApiWrapper.Convert(scp, _instances, _serviceProvider);
                ret = await _middlewareBuilder.InvokeAsync(rpcContext);
            }
            catch (Exception e)
            {
                //send fault
                await _convert.SendFaultAsync(e, rpcContext);
                return;
            }

            var hasStream = ret.TryGetStream(out System.IO.Stream retStream, out string retStreamName);

            if (hasStream)
                await _convert.SendStreamAsync(retStream, retStreamName);
            else
                await _convert.SendResultAsync(new CustomResult(ret, false, retStream.GetLength()), rpcContext);
        }
    }
}