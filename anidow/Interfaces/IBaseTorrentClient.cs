using System.Threading.Tasks;
using Anidow.Database.Models;

namespace Anidow.Interfaces
{
    public interface IBaseTorrentClient
    {
        Task<bool> Add(ITorrentItem item);
        Task<bool> Remove(Episode episode, bool withFile = false);
    }
}