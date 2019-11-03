using System.Threading.Tasks;

namespace Wikiled.Sentiment.Service.Logic.Allocation
{
    public interface IResourcesHandler
    {
        Task<bool> Allocate(string userId);

        void Release(string userId);
    }
}
