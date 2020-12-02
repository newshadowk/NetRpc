namespace System.Buffers
{
    public static class ArrayPoolExtensions
    {
        public static ArrayOwner<T> RentOwner<T>(this ArrayPool<T> arrayPool, int minimumLength)
        {
            return new(arrayPool.Rent(minimumLength));
        }
    }

    public class ArrayOwner<T> : IDisposable
    {
        public ArrayOwner(T[] array)
        {
            Array = array;
        }

        public T[] Array { get; }

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(Array);
        }
    }
}