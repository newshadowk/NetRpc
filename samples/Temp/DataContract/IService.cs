using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task<string> CallAsync(string s);
    }
}