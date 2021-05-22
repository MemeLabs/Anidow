using BencodeNET.Torrents;

namespace Anidow.Events
{
    public class DownloadEvent
    {
        public object Item { get; set; }
        public Torrent Torrent { get; set; }
    }
}