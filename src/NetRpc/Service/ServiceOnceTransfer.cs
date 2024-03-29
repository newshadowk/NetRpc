﻿using Microsoft.Extensions.Logging;
using NetRpc.Contract;

namespace NetRpc;

internal sealed class ServiceOnceTransfer
{
    private readonly IServiceOnceApiConvert _convert;
    private readonly List<Instance> _instances;
    private readonly IServiceProvider _serviceProvider;
    private readonly MiddlewareBuilder _middlewareBuilder;
    private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
    private readonly ChannelType _channelType;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _serviceCts = new();

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

    public Task<bool> StartAsync()
    {
        return _convert.StartAsync(_serviceCts);
    }

    public async Task HandleRequestAsync()
    {
        ActionExecutingContext? context = null;
        object? ret;

        try
        {
            //get context
            context = await GetContextAsync();

            //debug info
            GlobalDebugContext.Context.Info($"context:\r\n{context}");

            //set Accessor
            _actionExecutingContextAccessor.Context = context;

            //middleware Invoke
            ret = await _middlewareBuilder.InvokeAsync(context);
        }
        catch (Exception e)
        {
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

            return;
        }

        //if Post, do not need send back to client.
        if (context.ContractMethod.IsMQPost)
            return;

        var hasStream = ret.TryGetStream(out var retStream, out var retStreamName);

        try
        {
            //send result
            await _convert.SendResultAsync(new CustomResult(ret, hasStream, context.ContractMethod.IsImage, retStream.GetLength()), retStream, retStreamName,
                context);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "HandleRequestAsync SendResultAsync");
        }
    }

    private async Task<ActionExecutingContext> GetContextAsync()
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

                try
                {
                    _serviceCts.Cancel();
                }
                catch
                {
                    _logger.LogWarning(e, "_serviceCts.Cancel(), failed.");
                }
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
}