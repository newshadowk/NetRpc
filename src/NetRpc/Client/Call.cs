﻿using System.Reflection;

namespace NetRpc;

internal sealed class Call : ICall
{
    private readonly Guid _clientProxyId;
    private readonly ContractInfo _contract;
    private readonly IOnceCallFactory _factory;
    private readonly ClientMiddlewareBuilder _middlewareBuilder;
    private readonly string? _optionsName;
    private readonly IServiceProvider _serviceProvider;
    private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
    private volatile int _timeoutInterval;
    private readonly bool _forwardAllHeaders;
    private readonly List<string> _forwardHeaderKeys;
    private static readonly AsyncLocal<Dictionary<string, object?>> AsyncLocalHeader = new();

    public Call(Guid clientProxyId,
        ContractInfo contractInfo,
        IServiceProvider serviceProvider,
        ClientMiddlewareOptions middlewareOptions,
        IActionExecutingContextAccessor actionExecutingContextAccessor,
        IOnceCallFactory factory,
        Dictionary<string, object?> additionHeader,
        int timeoutInterval,
        bool forwardAllHeaders,
        List<string> forwardHeaderKeys,
        string? optionsName)
    {
        _clientProxyId = clientProxyId;
        _serviceProvider = serviceProvider;
        _actionExecutingContextAccessor = actionExecutingContextAccessor;
        _contract = contractInfo;
        _factory = factory;
        _timeoutInterval = timeoutInterval;
        _forwardAllHeaders = forwardAllHeaders;
        _forwardHeaderKeys = forwardHeaderKeys;
        _optionsName = optionsName;
        _middlewareBuilder = new ClientMiddlewareBuilder(middlewareOptions, serviceProvider);
        AdditionHeader = additionHeader;
    }

    public Dictionary<string, object?> AdditionHeader { get; }

    public static Dictionary<string, object?> AdditionContextHeader
    {
        get
        {
            if (AsyncLocalHeader.Value == null)
                AsyncLocalHeader.Value = new Dictionary<string, object?>();
            return AsyncLocalHeader.Value;
        }
        set => AsyncLocalHeader.Value = value;
    }

    public async Task<object?> CallAsync(MethodInfo methodInfo, bool isRetry, Func<object?, Task>? callback, CancellationToken token, Stream? stream,
        params object?[] pureArgs)
    {
        //merge header
        var mergeHeader = MergeHeader();

        var contractMethod = _contract.Methods.First(i => i.MethodInfo == methodInfo);

        //start
        var call = await _factory.CreateAsync(_timeoutInterval, isRetry);
        await call.StartAsync(mergeHeader, contractMethod.IsMQPost);

        //stream
        var instanceMethod = new InstanceMethod(methodInfo);
        var readStream = GetReadStream(stream);
        var clientContext = new ClientActionExecutingContext(_clientProxyId, _serviceProvider, _optionsName, call, instanceMethod, callback, token,
            _contract, contractMethod, readStream, mergeHeader, pureArgs);

        //invoke
        //onceTransfer will dispose after stream translate finished in OnceCall.
        await _middlewareBuilder.InvokeAsync(clientContext);
        return clientContext.Result!;
    }

    private static ProxyStream? GetReadStream(Stream? stream)
    {
        ProxyStream? proxyStream;
        switch (stream)
        {
            case null:
                proxyStream = null;
                break;
            case ProxyStream rs:
                proxyStream = rs;
                break;
            default:
                proxyStream = new ProxyStream(stream);
                break;
        }

        return proxyStream;
    }

    private Dictionary<string, object?> MergeHeader()
    {
        //_actionExecutingContextAccessor.Context?.Header is immutable here, should only change via middleware.
        var contextH = _actionExecutingContextAccessor.Context?.Header;
        var dic = new Dictionary<string, object?>();
        if (_forwardAllHeaders)
        {
            if (contextH != null)
                foreach (var key in contextH.Keys)
                    dic.Add(key, contextH[key]);
        }
        else if (_forwardHeaderKeys.Count > 0)
        {
            if (contextH != null)
                foreach (var key in contextH.Keys)
                {
                    if (_forwardHeaderKeys.Exists(i => i.ToLower() == key.ToLower()))
                        dic.Add(key, contextH[key]);
                }
        }

        foreach (var key in AdditionHeader.Keys)
            dic[key] = AdditionHeader[key];

        foreach (var key in AdditionContextHeader.Keys)
            dic[key] = AdditionContextHeader[key];

        return dic;
    }
}