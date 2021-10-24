namespace Anidow.Model;

public class QBitTorrentSettingsModel : ObservableObject
{
    public string Host { get; set; } = "http://localhost";
    public int Port { get; set; } = 8080;
    public string Category { get; set; } = "Anime";
    public bool WithLogin { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "adminadmin";
}