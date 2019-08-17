using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal abstract class ServiceOnceTransferBase
    {
        protected readonly IServiceOnceApiConvert Convert;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        protected ServiceOnceTransferBase(IServiceOnceApiConvert convert)
        {
            Convert = convert;
        }

        public void Start()
        {
            Convert.Start(_serviceCts);
        }

        public abstract Task HandleRequestAsync();

        protected async Task<ServiceCallParam> GetServiceCallParamAsync()
        {
            var onceCallParam = await Convert.GetOnceCallParamAsync();
            var stream = Convert.GetRequestStream(onceCallParam.StreamLength);
            ServiceCallParam serviceCallParam = new ServiceCallParam(onceCallParam,
                async i => await Convert.SendCallbackAsync(i),
                _serviceCts.Token, stream);
            return serviceCallParam;
        }
    }
}