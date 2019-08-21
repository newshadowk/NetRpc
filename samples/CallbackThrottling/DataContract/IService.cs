using System;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call(Action<int> cb);
    }
}