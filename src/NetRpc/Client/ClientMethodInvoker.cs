using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class ClientMethodInvoker : IMethodInvoker
    {
        private readonly ICall _call;

        public ClientMethodInvoker(ICall call)
        {
            _call = call;
        }

        public object Invoke(MethodInfo targetMethod, object[] args)
        {
            var (callback, token, stream, otherArgs) = GetArgs(args);
            token.ThrowIfCancellationRequested();
            try
            {
                return _call.CallAsync(targetMethod, callback, token, stream, otherArgs).Result;
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

        public Task InvokeAsync(MethodInfo targetMethod, object[] args)
        {
            var (callback, token, stream, otherArgs) = GetArgs(args);
            token.ThrowIfCancellationRequested();
            return _call.CallAsync(targetMethod, callback, token, stream, otherArgs);
        }

        public Task<T> InvokeAsyncT<T>(MethodInfo targetMethod, object[] args)
        {
            var (callback, token, stream, otherArgs) = GetArgs(args);
            token.ThrowIfCancellationRequested();
            return _call.CallAsync(targetMethod, callback, token, stream, otherArgs).ContinueWith(i => (T)i.Result);
        }

        private static (Action<object> callBack, CancellationToken token, Stream stream, object[] otherArgs) GetArgs(object[] args)
        {
            var objs = args.ToList();

            //cb
            Action<object> retCallback = null;
            var found = objs.FirstOrDefault(i =>
                i != null &&
                i.GetType().IsActionT());
            if (found != null)
            {
                retCallback = ActionHelper.ConvertAction(found);
                objs.Remove(found);
            }

            //token
            CancellationToken retToken;
            found = objs.FirstOrDefault(i => i is CancellationToken);
            if (found != null)
            {
                retToken = (CancellationToken) found;
                objs.Remove(found);
            }

            //stream
            Stream retStream = null;
            found = objs.FirstOrDefault(i => i is Stream);
            if (found != null)
            {
                retStream = (Stream) found;
                objs.Remove(found);
            }

            //otherArgs
            return (retCallback, retToken, retStream, objs.ToArray());
        }
    }
}