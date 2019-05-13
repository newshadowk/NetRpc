using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nrpc
{
    internal sealed class ServiceTransfer
    {
        private readonly ServiceApiConvert _convert;
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        public ServiceTransfer(IConnection connection, object[] instances, MiddlewareRegister middlewareRegister)
        {
            _convert = new ServiceApiConvert(connection, _serviceCts);
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
            var apiContext = ApiWrapper.Convert(scp, _instances);
            object ret;
            try
            {
                ret = await _middlewareRegister.InvokeAsync(new MiddlewareContext(apiContext));
            }
            catch (Exception e)
            {
                //send fault
                await _convert.SendFaultAsync(e);
                return;
            }

            //send result
            await _convert.SendResultAsync(ret);

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
                async i => await _convert.SendCallbackAsync(i),
                _serviceCts.Token, stream);
            return serviceCallParam;
        }
    }
}