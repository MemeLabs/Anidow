using System.Threading.Tasks;
using Anidow.Database.Models;
using Anidow.Model;

namespace Anidow.Interfaces
{
    public interface IBaseTorrentClient
    {
        Task<bool> Add(ITorrentItem item);
        Task<bool> Remove(Episode anime, bool withFile = false);
    }
}