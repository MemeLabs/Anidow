using System;
using Anidow.Torrent_Clients;

namespace Anidow.Factories;

public class TorrentClientFactory
{
    public TorrentClientFactory(QBitTorrent qBitTorrent)
    {
        GetQBitTorrent = qBitTorrent ?? throw new ArgumentNullException(nameof(qBitTorrent));
    }

    public QBitTorrent GetQBitTorrent { get; }
}