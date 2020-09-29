using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;

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
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        public ServiceOnceTransfer(List<Instance> instances, IServiceProvider serviceProvider, IServiceOnceApiConvert convert,
            MiddlewareBuilder middlewareBuilder, IActionExecutingContextAccessor actionExecutingContextAccessor,
            ChannelType channelType, ILogger logger)
        {
            _instances = instances;
            _serviceProvider = serviceProvider;
            _middlewareBuilder = middlewareBuilder;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _channelType = channelType;
            _logger = logger;
            _convert = convert;
        }

        public async Task StartAsync()
        {
            await _convert.StartAsync(_serviceCts);
        }

        public async Task HandleRequestAsync()
        {
            ActionExecutingContext? context = null;

            try
            {
                //get context
                context = await GetContext();

                //set Accessor
                _actionExecutingContextAccessor.Context = context;

                //middleware Invoke
                var ret = await _middlewareBuilder.InvokeAsync(context);

                //if Post, do not need send back to client.
                if (context.ContractMethod.IsMQPost)
                    return;

                var hasStream = ret.TryGetStream(out var retStream, out var retStreamName);

                //send result
                var sendStreamNext = await _convert.SendResultAsync(new CustomResult(ret, hasStream, retStream.GetLength()), retStream, retStreamName, context);
                if (!sendStreamNext)
                    return;

                //send stream
                await SendStreamAsync(context, hasStream, retStream);

                context.OnSendResultStreamFinished();
            }
            catch (Exception e)
            {
                //_logger.LogWarning(e, "HandleRequestAsync");

                //if Post, do not need send back to client.
                if (context != null && context.ContractMethod.IsMQPost)
                    return;

                //send fault
                try
                {
                    await _convert.SendFaultAsync(e, context);
                }
                catch (Exception e2)
                {
                    _logger.LogWarning(e2, "HandleRequestAsync SendFaultAsync");
                }
            }
        }

        private async Task<ActionExecutingContext> GetContext()
        {
            var onceCallParam = await _convert.GetServiceOnceCallParamAsync();
            var (instanceMethodInfo, contractMethod, instance) = ApiWrapper.GetMethodInfo(onceCallParam.Action, _instances);

            //get parameters
            var parameters = contractMethod.MethodInfo.GetParameters();

            //callback, cancel when exception
            async Task Callback(object? i)
            {
                try
                {
                    if (!_serviceCts.IsCancellationRequested)
                        await _convert.SendCallbackAsync(i);
                    else
                        _logger.LogWarning("Call back ignored, action is canceled.");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Callback error, cancel action.");
                    _serviceCts.Cancel();
                    throw;
                }
            }

            //args
            var args = ApiWrapper.GetArgs(parameters, onceCallParam.PureArgs, Callback, _serviceCts.Token, onceCallParam.Stream);

            return new ActionExecutingContext(
                _serviceProvider,
                onceCallParam.Header,
                instance,
                instanceMethodInfo,
                contractMethod,
                args,
                onceCallParam.PureArgs,
                onceCallParam.Action,
                onceCallParam.Stream,
                instance.Contract,
                _channelType,
                Callback,
                _serviceCts.Token);
        }

        private async Task SendStreamAsync(ActionExecutingContext context, bool hasStream, Stream? retStream)
        {
            if (hasStream)
            {
                try
                {
#if NETSTANDARD2_1 || NETCOREAPP3_1
                    await
#endif

                    using (retStream)
                    {
                        await Helper.SendStreamAsync(
                            i => _convert.SendBufferAsync(i), 
                            () => _convert.SendBufferEndAsync(), 
                            retStream!, 
                            context.CancellationToken, 
                            context.OnSendResultStreamStarted);
                    }
                }
                catch (TaskCanceledException)
                {
                    await _convert.SendBufferCancelAsync();
                }
                catch
                {
                    await _convert.SendBufferFaultAsync();
                }
            }
        }
    }
}