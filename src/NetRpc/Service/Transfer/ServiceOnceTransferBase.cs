using System.IO;
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
            //onceCallParam
            var onceCallParam = await Convert.GetOnceCallParamAsync();

            //stream
            Stream stream;
            if (onceCallParam.Action.IsPost)
                stream = BytesToStream(onceCallParam.PostStream);
            else
                stream = Convert.GetRequestStream(onceCallParam.StreamLength);

            //serviceCallParam
            return new ServiceCallParam(onceCallParam,
                async i => await Convert.SendCallbackAsync(i),
                _serviceCts.Token, stream);
        }

        private static Stream BytesToStream(byte[] bytes)
        {
            if (bytes == null)
                return null;
            Stream stream = new MemoryStream(bytes);
            return stream;
        }
    }
}