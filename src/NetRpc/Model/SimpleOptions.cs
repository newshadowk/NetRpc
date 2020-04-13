using Microsoft.Extensions.Options;

namespace NetRpc
{
    public sealed class SimpleOptions<T> : IOptions<T> where T : class, new()
    {
        public SimpleOptions(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}