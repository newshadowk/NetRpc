using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal abstract class ServiceOnceTransferBase
    {
        private readonly IServiceOnceApiConvert _convert;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        protected ServiceOnceTransferBase(IServiceOnceApiConvert convert)
        {
            _convert = convert;
        }

        public void Start()
        {
            _convert.Start(_serviceCts);
        }

        public abstract Task HandleRequestAsync();

        protected async Task<ServiceCallParam> GetServiceCallParamAsync()
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