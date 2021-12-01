using System;
using System.Collections.Generic;

namespace NetRpc;

internal sealed class CallFactory
{
    private readonly Type _tService;
    private readonly Guid _id;
    private readonly IServiceProvider _serviceProvider;
    private readonly ClientMiddlewareOptions _options;
    private readonly IActionExecutingContextAccessor _accessor;
    private readonly IOnceCallFactory _onceCallFactory;
    private readonly NClientOptions _nClientOptions;
    private readonly Dictionary<string, object?> _additionHeader;
    private readonly string? _optionsName;

    public CallFactory(Type tService, Guid id, IServiceProvider serviceProvider,
        ClientMiddlewareOptions options,
        IActionExecutingContextAccessor accessor,
        IOnceCallFactory onceCallFactory,
        NClientOptions nClientOptions,
        Dictionary<string, object?> additionHeader,
        string? optionsName)
    {
        _tService = tService;
        _id = id;
        _serviceProvider = serviceProvider;
        _options = options;
        _accessor = accessor;
        _onceCallFactory = onceCallFactory;
        _nClientOptions = nClientOptions;
        _additionHeader = additionHeader;
        _optionsName = optionsName;
    }

    public Call Create()
    {
        return new(_id,
            ClientContractInfoCache.GetOrAdd(_tService),
            _serviceProvider,
            _options,
            _accessor,
            _onceCallFactory,
            _additionHeader,
            _nClientOptions.TimeoutInterval,
            _nClientOptions.ForwardAllHeaders,
            _nClientOptions.ForwardHeaderKeys,
            _optionsName);
    }
}