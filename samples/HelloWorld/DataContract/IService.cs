using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call(string s);
    }
}