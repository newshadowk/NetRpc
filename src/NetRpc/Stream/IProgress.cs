using System;

namespace NetRpc
{
    public interface IProgress
    {
        event EventHandler Finished;
        event EventHandler Started;
        event EventHandler<long> Progress;
    }
}