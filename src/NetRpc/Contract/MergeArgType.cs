using System;

namespace NetRpc
{
    public sealed class MergeArgType
    {
        public Type Type { get; }

        public Type TypeWithoutStreamName { get; }

        public string StreamPropName { get; }

        public TypeName CallbackAction { get; }

        public TypeName CancelToken { get; }

        public MergeArgType(Type type, Type typeWithoutStreamName, string streamPropName, TypeName callbackAction, TypeName cancelToken)
        {
            Type = type;
            StreamPropName = streamPropName;
            CallbackAction = callbackAction;
            CancelToken = cancelToken;
            TypeWithoutStreamName = typeWithoutStreamName;
        }
    }
}