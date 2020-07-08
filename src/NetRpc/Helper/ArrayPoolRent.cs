using System;
using System.Buffers;

namespace NetRpc
{
    public class ArrayPoolRent<T> :IDisposable
    {
        public ArrayPoolRent(int minimumLength)
        {
            Data = ArrayPool<T>.Shared.Rent(minimumLength);
        }

        public T[] Data { get; }

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(Data);
        }
    }
}