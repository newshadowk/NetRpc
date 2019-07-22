using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class BufferServiceOnceTransfer : ServiceOnceTransferBase
    {
        private readonly IBufferServiceOnceApiConvert _convert;
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;

        public BufferServiceOnceTransfer(IBufferServiceOnceApiConvert convert, MiddlewareRegister middlewareRegister, object[] instances) : base(convert)
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

            var hasStream = ret.TryGetStream(out System.IO.Stream retStream, out _);

            //send result
            await _convert.SendResultAsync(new CustomResult(ret, hasStream, retStream.GetLength()), scp.Action, scp.Args);

            //send stream
            if (hasStream)
            {
                try
                {
                    await Helper.SendStreamAsync(i => _convert.SendBufferAsync(i), () =>
                        _convert.SendBufferEndAsync(), retStream, scp.Token);
                }
                catch (TaskCanceledException)
                {
                    await _convert.SendBufferCancelAsync();
                }
                catch (Exception)
                {
                    await _convert.SendBufferFaultAsync();
                }
            }
        }
    }
}