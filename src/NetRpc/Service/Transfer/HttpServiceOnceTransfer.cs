using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class HttpServiceOnceTransfer : ServiceOnceTransferBase
    {
        private readonly IHttpServiceOnceApiConvert _convert;
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;

        public HttpServiceOnceTransfer(IHttpServiceOnceApiConvert convert, MiddlewareRegister middlewareRegister, object[] instances) 
            : base(convert)
        {
            _convert = convert;
            _instances = instances;
            _middlewareRegister = middlewareRegister;
        }

        public override async Task HandleRequestAsync()
        {
            ServiceCallParam scp = null;
            object ret;

            try
            {
                scp = await GetServiceCallParamAsync();
                var apiContext = ApiWrapper.Convert(scp, _instances);
                ret = await _middlewareRegister.InvokeAsync(new MiddlewareContext(apiContext));
            }
            catch (Exception e)
            {
                //send fault
                if (scp == null)
                    await _convert.SendFaultAsync(e, null, null);
                else
                    await _convert.SendFaultAsync(e, scp.Action, scp.Args);
                return;
            }

            var hasStream = ret.TryGetStream(out System.IO.Stream retStream, out string retStreamName);

            if (hasStream)
                await _convert.SendStreamAsync(retStream, retStreamName);
            else
                await _convert.SendResultAsync(new CustomResult(ret, false, retStream.GetLength()), scp.Action, scp.Args);
        }
    }
}