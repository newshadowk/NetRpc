using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class BufferServiceOnceTransfer : ServiceOnceTransferBase
    {
        private readonly IBufferServiceOnceApiConvert _convert;
        private readonly object[] _instances;
        private readonly IServiceProvider _serviceProvider;
        private readonly MiddlewareBuilder _middlewareBuilder;

        public BufferServiceOnceTransfer(object[] instances, IServiceProvider serviceProvider, IBufferServiceOnceApiConvert convert,
            MiddlewareBuilder middlewareBuilder) : base(convert)
        {
            _instances = instances;
            _serviceProvider = serviceProvider;
            _convert = (IBufferServiceOnceApiConvert) Convert;
            _middlewareBuilder = middlewareBuilder;
        }

        public override async Task HandleRequestAsync()
        {
            object ret;
            RpcContext rpcContext = null;
            ServiceCallParam scp;

            try
            {
                scp = await GetServiceCallParamAsync();
                rpcContext = ApiWrapper.Convert(scp, _instances, _serviceProvider);
                ret = await _middlewareBuilder.InvokeAsync(rpcContext);
            }
            catch (Exception e)
            {
                //send fault
                await _convert.SendFaultAsync(e, rpcContext);
                return;
            }

            var hasStream = ret.TryGetStream(out var retStream, out _);

            //send result
            await _convert.SendResultAsync(new CustomResult(ret, hasStream, retStream.GetLength()), rpcContext);

            //send stream
            if (hasStream)
            {
                try
                {
                    using (retStream)
                    {
                        await Helper.SendStreamAsync(i => _convert.SendBufferAsync(i), () =>
                            _convert.SendBufferEndAsync(), retStream, scp.Token);
                    }
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