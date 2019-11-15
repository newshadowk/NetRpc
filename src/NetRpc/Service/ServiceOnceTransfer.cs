using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class ServiceOnceTransfer
    {
        private readonly IServiceOnceApiConvert _convert;
        private readonly List<Instance> _instances;
        private readonly IServiceProvider _serviceProvider;
        private readonly MiddlewareBuilder _middlewareBuilder;
        private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
        private readonly ChannelType _channelType;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        public ServiceOnceTransfer(List<Instance> instances, IServiceProvider serviceProvider, IServiceOnceApiConvert convert,
            MiddlewareBuilder middlewareBuilder, IActionExecutingContextAccessor actionExecutingContextAccessor, ChannelType channelType)
        {
            _instances = instances;
            _serviceProvider = serviceProvider;
            _middlewareBuilder = middlewareBuilder;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _channelType = channelType;
            _convert = convert;
        }

        public async Task StartAsync()
        {
            await _convert.StartAsync(_serviceCts);
        }

        public async Task HandleRequestAsync()
        {
            object ret;
            ActionExecutingContext context = null;

            try
            {
                //get context
                context = await GetContext();

                //set Accessor
                _actionExecutingContextAccessor.Context = context;

                //middleware Invoke
                ret = await _middlewareBuilder.InvokeAsync(context);

                //if Post, do not need send back to client.
                if (context.ContractMethod.IsMQPost)
                    return;
            }
            catch (Exception e)
            {
                //if Post, do not need send back to client.
                if (context != null && context.ContractMethod.IsMQPost)
                    return;

                //send fault
                await _convert.SendFaultAsync(e, context);
                return;
            }

            var hasStream = ret.TryGetStream(out var retStream, out var retStreamName);

            //send result
            var sendStreamNext = await _convert.SendResultAsync(new CustomResult(ret, hasStream, retStream.GetLength()), retStream, retStreamName, context);
            if (!sendStreamNext)
                return;

            //send stream
            await SendStreamAsync(context, hasStream, retStream);

            context.OnSendResultStreamFinished();
        }

        private async Task<ActionExecutingContext> GetContext()
        {
            var onceCallParam = await _convert.GetOnceCallParamAsync();
            var (instanceMethodInfo, contractMethod, instance) = ApiWrapper.GetMethodInfo(onceCallParam.Action, _instances, _serviceProvider);

            //get parameters
            var parameters = contractMethod.MethodInfo.GetParameters();

            //stream
            Stream stream = null;
            if (contractMethod.IsMQPost)
                stream = BytesToStream(onceCallParam.PostStream);
            else
            {
                var hasStream = parameters.Any(i => i.ParameterType == typeof(Stream));
                if (hasStream)
                    stream = _convert.GetRequestStream(onceCallParam.StreamLength);
            }

            //serviceCallParam
            var scp = new ServiceCallParam(onceCallParam,
                async i => await _convert.SendCallbackAsync(i),
                _serviceCts.Token, stream);

            var args = ApiWrapper.GetArgs(parameters, scp.PureArgs, scp.Callback, scp.Token, scp.Stream);
            return new ActionExecutingContext(
                _serviceProvider,
                scp.Header,
                instance,
                instanceMethodInfo,
                contractMethod,
                args,
                scp.PureArgs,
                scp.Action,
                scp.Stream,
                instance.Contract,
                _channelType,
                scp.Callback,
                scp.Token);
        }

        private async Task SendStreamAsync(ActionExecutingContext context, bool hasStream, Stream retStream)
        {
            if (hasStream)
            {
                try
                {
                    using (retStream)
                    {
                        await Helper.SendStreamAsync(i => _convert.SendBufferAsync(i), () =>
                            _convert.SendBufferEndAsync(), retStream, context.CancellationToken, context.OnSendResultStreamStarted);
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

        private static Stream BytesToStream(byte[] bytes)
        {
            if (bytes == null)
                return null;
            Stream stream = new MemoryStream(bytes);
            return stream;
        }
    }
}