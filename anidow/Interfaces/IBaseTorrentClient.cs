using System.Threading.Tasks;

namespace Anidow.Interfaces
{
    public interface IBaseTorrentClient
    {
        Task<bool> Add(ITorrentItem item);
        Task<bool> Remove(IEpisode episode, bool withFile = false);
    }
}