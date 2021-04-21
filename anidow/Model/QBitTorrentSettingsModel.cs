namespace Anidow.Model
{
    public class QBitTorrentSettingsModel : ObservableObject
    {
        public string Host { get; set; } = "http://localhost";
        public int Port { get; set; } = 1584;
        public string Category { get; set; } = "Anime";
        public bool WithLogin { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}