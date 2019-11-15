using System;
using Microsoft.Extensions.Logging;
using OpenTracing.Util;

namespace NetRpc.OpenTracing
{
    public class SpanLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var span = GlobalTracer.Instance.ActiveSpan;
            if (span != null)
            {
                var msg = (formatter ?? Format).Invoke(state, exception);
                span.Log(msg);
            }
        }

        private static string Format<TState>(TState state, Exception e)
        {
            return $"{state}{Helper.GetException(e)}";
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
