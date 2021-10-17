using BencodeNET.Torrents;

namespace Anidow.Events;

public class DownloadEvent
{
    public object Item { get; init; }
    public Torrent Torrent { get; init; }
}