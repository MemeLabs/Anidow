using System.Threading.Tasks;
using BencodeNET.Torrents;

namespace Anidow.Interfaces;

public interface IBaseTorrentClient
{
    Task<bool> Add(ITorrentItem item, Torrent torrent = null);
    Task<bool> Remove(IEpisode episode, bool withFile = false);
}