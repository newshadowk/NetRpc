using System;

namespace NetRpc
{
    public interface IProgress
    {
        event EventHandler<SizeEventArgs> Finished;
        event EventHandler Started;
        event EventHandler<SizeEventArgs> Progress;
    }
}