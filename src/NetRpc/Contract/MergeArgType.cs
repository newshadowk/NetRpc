﻿using System.Reflection;

namespace NetRpc;

public sealed class MergeArgType
{
    public MethodInfo MethodInfo { get; }

    public Type? Type { get; }

    public Type? TypeWithoutPathQueryStream { get; }

    public string? StreamPropName { get; }

    public TypeName? CallbackAction { get; }

    public TypeName? CancelToken { get; }

    public bool IsSingleValue { get; }

    public ParameterInfo? SingleValue { get; }

    public MergeArgType(Type? type, Type? typeWithoutPathQueryStream, string? streamPropName, TypeName? callbackAction, TypeName? cancelToken, 
        bool isSingleValue, ParameterInfo? singleValue, MethodInfo methodInfo)
    {
        Type = type;
        StreamPropName = streamPropName;
        CallbackAction = callbackAction;
        CancelToken = cancelToken;
        IsSingleValue = isSingleValue;
        SingleValue = singleValue;
        MethodInfo = methodInfo;
        TypeWithoutPathQueryStream = typeWithoutPathQueryStream;
    }
}