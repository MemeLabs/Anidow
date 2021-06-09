// // Created: 06-06-2021 14:53

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Anidow.Helpers;
using Anidow.Model;
using Anidow.Services;
using Anidow.Torrent_Clients;
using Anidow.Utils;
using Screen = Stylet.Screen;

using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace Anidow.Pages.Components.Settings
{
    public class SettingsSetupWizardViewModel : Screen
    {

        private readonly Regex _regex = new("[^0-9]+");
        private readonly SettingsService _settingsService;

        private readonly string _iniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "qBittorrent", "qBittorrent.ini");

        private const int Steps = 2;

        public SettingsSetupWizardViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService ?? throw  new ArgumentNullException(nameof(settingsService));
        }

        public SettingsModel Settings => _settingsService.TempSettings;

        public int CurrentStep { get; private set; } = 1;
        public bool CanBack => CurrentStep >= 2;
        public bool CanNext => CurrentStep <= Steps;
        public bool IsFinish => CurrentStep == Steps;

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

        public bool CanQuickSetupQBittorrent { get; set; }
        public void QuickSetupQBittorrent()
        {
            if (ProcessUtil.IsRunning("qbittorrent"))
            {
                MessageBox.Show("Please close qBittorrent before running quick setup.", "qBittorrent running!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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
    }
}