using System;
using System.IO;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task CallAsync(Stream s, Action<int> cb, string s1);
    }
}