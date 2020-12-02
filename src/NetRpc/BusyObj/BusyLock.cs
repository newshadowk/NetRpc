namespace NetRpc
{
    internal sealed class BusyLock
    {
        private readonly object _lockObj = new();

        private volatile bool _isLocked;

        public BusyLockToken Lock()
        {
            lock (_lockObj)
            {
                if (_isLocked)
                    return new BusyLockToken(false, this);
                _isLocked = true;
                return new BusyLockToken(true, this);
            }
        }

        public void Release()
        {
            lock (_lockObj)
            {
                _isLocked = false;
            }
        }
    }
}