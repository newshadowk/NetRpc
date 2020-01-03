using System;
using System.Timers;

namespace NetRpc
{
    internal class BusyTimer : IDisposable
    {
        private readonly Timer T = new Timer();
        private readonly BusyLock TLock = new BusyLock();

        public double Interval => T.Interval;

        public BusyTimer(double interval)
        {
            T.Elapsed += T_Elapsed;
            T.Interval = interval;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            T.Dispose();
        }

        public void Start(bool isImmediately = false)
        {
            TryStart();
            if (isImmediately)
                DoElapsed(null);
        }

        public void Stop()
        {
            TryStop();
        }

        public event ElapsedEventHandler Elapsed;

        private void TryStart()
        {
            //Possible T is already Disposed.
            try
            {
                T.Start();
            }
            catch
            {
            }
        }

        private void TryStop()
        {
            //Possible T is already Disposed.
            try
            {
                T.Stop();
            }
            catch
            {
            }
        }

        private void OnElapsed(ElapsedEventArgs e)
        {
            Elapsed?.Invoke(this, e);
        }

        private void DoElapsed(ElapsedEventArgs e)
        {
            using (var token = TLock.Lock())
            {
                if (!token.LockGot)
                    return;
                TryStop();
                OnElapsed(e);
                TryStart();
            }
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            DoElapsed(e);
        }

        ~BusyTimer()
        {
            Dispose(false);
        }
    }
}