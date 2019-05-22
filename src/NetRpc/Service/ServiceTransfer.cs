using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class ServiceTransfer
    {
        private readonly ServiceApiConvert _convert;
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        public ServiceTransfer(IConnection connection, MiddlewareRegister middlewareRegister, bool isWrapFaultException, object[] instances)
        {
            _convert = new ServiceApiConvert(connection, isWrapFaultException, _serviceCts);
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

            //send result
            await _convert.SendResultAsync(ret, scp.Action, scp.Args);

            //send stream
            if (ret.TryGetStream(out Stream retStream))
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
            var stream = _convert.GetRequestStream();
            ServiceCallParam serviceCallParam = new ServiceCallParam(onceCallParam,
                async i => await _convert.SendCallbackAsync(i, onceCallParam.Action, onceCallParam.Args),
                _serviceCts.Token, stream);
            return serviceCallParam;
        }
    }
}