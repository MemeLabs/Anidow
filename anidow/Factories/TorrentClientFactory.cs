using Anidow.Torrent_Clients;

namespace Anidow.Factories
{
    public class TorrentClientFactory
    {
        public TorrentClientFactory(QBitTorrent qBitTorrent)
        {
            GetQBitTorrent = qBitTorrent;
        }

        public QBitTorrent GetQBitTorrent { get; }
    }
}