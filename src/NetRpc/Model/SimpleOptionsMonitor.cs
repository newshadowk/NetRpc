using System;
using Microsoft.Extensions.Options;

namespace NetRpc
{
    public class SimpleOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        public SimpleOptionsMonitor(TOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public TOptions Get(string name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            return null;
        }

        public TOptions CurrentValue { get; }
    }
}