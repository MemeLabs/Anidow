using System.IO;

namespace Anidow.Model;

public class AnimeBytesSettingsModel : ObservableObject
{
    public string PassKey { get; set; }
    public string Username { get; set; }
    public string DefaultDownloadFolder { get; set; } = Directory.GetCurrentDirectory();
    public int HideTorrentsBelowSeeders { get; set; } = -1;
}