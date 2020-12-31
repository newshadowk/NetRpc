using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;
using Polly;

namespace NetRpc
{
    internal sealed class ClientMethodRetryInvoker : IMethodInvoker
    {
        private readonly CallFactory _callFactory;
        private readonly TimeSpan[]? _serviceTypeSleepDurations;
        private readonly ILogger _logger;

        public ClientMethodRetryInvoker(CallFactory callFactory, TimeSpan[]? serviceTypeSleepDurations, ILogger logger)
        {
            _callFactory = callFactory;
            _serviceTypeSleepDurations = serviceTypeSleepDurations;
            _logger = logger;
        }

        public object? Invoke(MethodInfo targetMethod, object?[] args)
        {
            try
            {
                return CallAsync(targetMethod, args).Result;
            }
            catch (AggregateException e)
            {
                if (e.InnerException != null)
                {
                    var edi = ExceptionDispatchInfo.Capture(e.InnerException);
                    edi.Throw();
                }

                throw;
            }
        }

        public async Task InvokeAsync(MethodInfo targetMethod, object?[] args)
        {
            await CallAsync(targetMethod, args);
        }

        public async Task<T> InvokeAsyncT<T>(MethodInfo targetMethod, object?[] args)
        {
            var ret = await CallAsync(targetMethod, args);
            if (ret == null)
                return default!;
            return (T)ret;
        }

        private async Task<object?> CallAsync(MethodInfo targetMethod, object?[] args)
        {
            var (callback, token, stream, otherArgs) = GetArgs(args);
            token.ThrowIfCancellationRequested();

            var sleepDurations = GetSleepDurations(targetMethod);

            //single call
            if (sleepDurations == null || !sleepDurations.Any())
            {
                var call = _callFactory.Create();
                return await call.CallAsync(targetMethod, callback, token, stream, otherArgs);
            }

            //retry call
            var p = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(sleepDurations, (exception, span, retryCount) =>
                {
                    _logger.LogWarning($"retry count:{retryCount}, span:{span.TotalMilliseconds}", exception);
                });

            return await p.ExecuteAsync(async (c, t) =>
            {
                var call = _callFactory.Create();
                return await call.CallAsync(targetMethod, callback, t, stream, otherArgs);
            }, new Context("call"), token);
        }

        private TimeSpan[]? GetSleepDurations(MethodInfo targetMethod)
        {
            var retry = targetMethod.GetCustomAttribute<ClientRetryAttribute>();
            if (retry != null)
                return retry.SleepDurations.ToList().ConvertAll(i => TimeSpan.FromMilliseconds(i)).ToArray();
            return _serviceTypeSleepDurations;
        }

        private static (Func<object?, Task>? callback, CancellationToken token, Stream? stream, object?[] otherArgs) GetArgs(object?[] args)
        {
            var objs = args.ToList();

            //callback
            Func<object?, Task>? retCallback = null;
            var found = objs.FirstOrDefault(i =>
                i != null &&
                i.GetType().IsFuncT());
            if (found != null)
            {
                retCallback = FuncHelper.ConvertFunc(found);
                objs.Remove(found);
            }

            //token
            var retToken = CancellationToken.None;
            found = objs.FirstOrDefault(i => i is CancellationToken);
            if (found != null)
            {
                retToken = (CancellationToken)found;
                objs.Remove(found);
            }

            //stream
            Stream? retStream = null;
            found = objs.FirstOrDefault(i => i is Stream);
            if (found != null)
            {
                retStream = (Stream)found;
                objs.Remove(found);
            }

            //otherArgs
            return (retCallback, retToken, retStream, objs.ToArray());
        }
    }
}