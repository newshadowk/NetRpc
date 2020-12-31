using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract
{
    [ClientRetry(1000, 2000, 3000)]
    public interface IServiceAsync
    {
        Task CallAsync(string s);
    }
}