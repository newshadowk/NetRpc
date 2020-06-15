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

    public sealed class NullOptions<T> : IOptions<T> where T : class, new()
    {
        public NullOptions()
        {
            Value = new T();
        }

        public T Value { get; }
    }
}