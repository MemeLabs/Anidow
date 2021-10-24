// // Created: 06-06-2021 14:53

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using AdonisUI.Controls;
using Anidow.Helpers;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using MessageBox = AdonisUI.Controls.MessageBox;
using Screen = Stylet.Screen;

namespace Anidow.Pages.Components.Settings;

public class SettingsSetupWizardViewModel : Screen
{
    private const int Steps = 2;

    private readonly string _iniPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "qBittorrent", "qBittorrent.ini");

    private readonly Regex _regex = new("[^0-9]+");
    private readonly SettingsService _settingsService;
    private readonly TorrentService _torrentService;

    public SettingsSetupWizardViewModel(SettingsService settingsService, TorrentService torrentService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
    }

    public SettingsModel Settings => _settingsService.TempSettings;

    public int CurrentStep { get; private set; } = 1;
    public bool CanBack => CurrentStep >= 2;
    public bool CanNext => CurrentStep <= Steps;
    public bool IsFinish => CurrentStep == Steps;

    public bool CanQuickSetupQBittorrent { get; set; }
    public string ConnectionStatus { get; set; }

    public bool CanFinish { get; set; }

    public void Back()
    {
        CurrentStep -= 1;
    }

    public void Next()
    {
        CurrentStep += 1;
        if (CurrentStep == 2)
        {
            if (File.Exists(_iniPath))
            {
                CanQuickSetupQBittorrent = true;
            }
        }
    }

    public void QuickSetupQBittorrent()
    {
        if (ProcessUtil.IsRunning("qbittorrent"))
        {
            MessageBox.Show("Please close qBittorrent before running quick setup.", "qBittorrent running!",
                MessageBoxButton.OK, MessageBoxImage.Asterisk);
            return;
        }

        CanQuickSetupQBittorrent = false;
        if (File.Exists(_iniPath))
        {
            var ini = new IniFile(_iniPath);
            var isEnabled = ini.Read(@"WebUI\Enabled", "Preferences");
            if (isEnabled == "false")
            {
                ini.Write(@"WebUI\Enabled", "true", "Preferences");
            }

            var host = ini.Read(@"WebUI\Address", "Preferences");
            var port = ini.Read(@"WebUI\Port", "Preferences");
            var withLocalHostAuth = ini.Read(@"WebUI\LocalHostAuth", "Preferences");
            var username = ini.Read(@"WebUI\Username", "Preferences");

            if (host == "*")
            {
                host = "http://localhost";
            }

            Settings.QBitTorrent.Host = host;
            Settings.QBitTorrent.Port = int.Parse(port);
            Settings.QBitTorrent.WithLogin = withLocalHostAuth == "true";
            Settings.QBitTorrent.Username = username;

            if (Settings.QBitTorrent.WithLogin && string.IsNullOrWhiteSpace(Settings.QBitTorrent.Password))
            {
                MessageBox.Show("Please manually input the password", "Password");
            }
        }

        CanQuickSetupQBittorrent = true;
    }

    public void Finish()
    {
        Task.Run(async () => await _settingsService.Save());
        Cancel();
    }

    public void Cancel()
    {
        RequestClose();
        CurrentStep = 1;
    }


    private string OpenFolderBrowserDialog(string path)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = path,
        };
        var result = dialog.ShowDialog();
        return result == DialogResult.OK ? dialog.SelectedPath : path;
    }

    public void SetAnimeFolder()
    {
        var folder = OpenFolderBrowserDialog(Settings.AnimeFolder);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        Settings.AnimeFolder = folder;
    }


    public void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = _regex.IsMatch(e.Text);
    }

    public async Task TestConnection()
    {
        var (success, error) = await _torrentService.TestConnection(Settings);
        if (success && string.IsNullOrEmpty(error))
        {
            CanFinish = true;
            ConnectionStatus = "Success!";
            return;
        }

        ConnectionStatus = "Failed!";
    }
}