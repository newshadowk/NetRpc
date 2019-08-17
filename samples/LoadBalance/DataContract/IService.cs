using System;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task CallAsync(Action<int> cb, string s1);
    }
}