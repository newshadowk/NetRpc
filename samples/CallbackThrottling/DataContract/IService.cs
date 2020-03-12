using System;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call(Func<int, Task> cb);
    }
}