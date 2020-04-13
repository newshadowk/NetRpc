using System.Threading;

namespace NetRpc
{
    public sealed class BusyFlag
    {
        private int _handlingCount;

        public bool IsHandling => _handlingCount > 0;

        public void Increment()
        {
            Interlocked.Increment(ref _handlingCount);
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref _handlingCount);
        }
    }
}