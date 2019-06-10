using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class ServiceOnceTransfer
    {
        private readonly ServiceOnceApiConvert _convert;
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        public ServiceOnceTransfer(IConnection connection, MiddlewareRegister middlewareRegister, object[] instances)
        {
            _convert = new ServiceOnceApiConvert(connection, _serviceCts);
            _instances = instances;
            _middlewareRegister = middlewareRegister;
        }

        public void Start()
        {
            _convert.Start();
        }

        public async Task HandleRequestAsync()
        {
            ServiceCallParam scp = await GetServiceCallParamAsync();
            object ret;

            try
            {
                var apiContext = ApiWrapper.Convert(scp, _instances);
                ret = await _middlewareRegister.InvokeAsync(new MiddlewareContext(apiContext));
            }
            catch (Exception e)
            {
                //send fault
                await _convert.SendFaultAsync(e, scp.Action, scp.Args);
                return;
            }

            var hasStream = ret.TryGetStream(out Stream retStream);

            //send result
            await _convert.SendResultAsync(new CustomResult(ret, retStream.GetLength()), scp.Action, scp.Args);

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

        private async Task<ServiceCallParam> GetServiceCallParamAsync()
        {
            var onceCallParam = await _convert.GetOnceCallParamAsync();
            var stream = _convert.GetRequestStream(onceCallParam.StreamLength);
            ServiceCallParam serviceCallParam = new ServiceCallParam(onceCallParam,
                async i => await _convert.SendCallbackAsync(i, onceCallParam.Action, onceCallParam.Args),
                _serviceCts.Token, stream);
            return serviceCallParam;
        }
    }
}