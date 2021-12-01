using System;
using System.Threading.Tasks;

namespace DataContract;

public interface IServiceAsync
{
    Task CallAsync(Func<int, Task> cb);
}