using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using Serilog;
using Screen = Stylet.Screen;
using TextBox = System.Windows.Controls.TextBox;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsViewModel : Screen
    {
        private readonly ILogger _logger;
        private readonly Regex _regex = new("[^0-9]+");

        public SettingsViewModel(ILogger logger, SettingsService settingsService)
        {
            SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            DisplayName = "Settings";
        }

        public SettingsService SettingsService { get; }
        public SettingsModel Settings => SettingsService.TempSettings;

        public void SetAnimeFolder()
        {
            var folder = OpenFolderBrowserDialog();
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            Settings.AnimeFolder = folder;
        }

        private string OpenFolderBrowserDialog()
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Directory.GetCurrentDirectory(),
            };
            var result = dialog.ShowDialog();
            return result == DialogResult.OK ? dialog.SelectedPath : default;
        }

        public void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }

        public void RefreshTimeTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "1";
            }
        }

        public void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                LinkUtil.Open(e.Uri.AbsoluteUri);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "failed opening link to passkey");
            }
        }
    }
}