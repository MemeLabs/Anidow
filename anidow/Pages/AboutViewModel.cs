using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Anidow.Helpers;
using Anidow.Model;
using Anidow.Utils;
using Newtonsoft.Json;
using Onova.Models;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    public class AboutViewModel : Screen
    {
        private readonly Assembly _assembly;
        private readonly ILogger _logger;
        private readonly UpdateManager _updateManager;
        private CheckForUpdatesResult _lastCheck;

        public AboutViewModel(Assembly assembly, UpdateManager updateManager, ILogger logger)
        {
            _assembly = assembly;
            _updateManager = updateManager;
            _logger = logger;
            DisplayName = "About";
        }


        public IObservableCollection<Package> Packages { get; } = new BindableCollection<Package>();

        public string Copyright => $"© 2020-{DateTime.Now.Year} MemeLabs";
        public string ProjectUrl => "https://github.com/MemeLabs/Anidow";

        public string AssemblyVersionString =>
            $"{_assembly.GetName().Version} {(Environment.Is64BitProcess ? "(x64)" : "(x32)")}";

        public string Product => "Anidow";
        public string UpdateMessage { get; private set; }
        public bool HasUpdate { get; private set; }

        public bool CanCheckForUpdate { get; set; } = true;

        public bool CanUpdateNow { get; private set; }

        protected override async void OnInitialActivate()
        {
            var licenses = _assembly.GetManifestResourceNames().Single(p => p.EndsWith("licenses.json"));
            await using var stream = _assembly.GetManifestResourceStream(licenses);
            using var reader = new StreamReader(stream!);
            var json = await reader.ReadToEndAsync();
            Packages.AddRange(JsonConvert.DeserializeObject<Package[]>(json));

            await CheckForUpdate();
        }

        public void OpenProjectUrl()
        {
            LinkUtil.Open(ProjectUrl);
        }

        public async Task CheckForUpdate()
        {
            CanCheckForUpdate = false;
            var (check, hasUpdate) = await _updateManager.HasUpdate();
            if (!hasUpdate)
            {
                UpdateMessage = "You have the latest version";
            }
            else if (check is not null)
            {
                HasUpdate = true;
                UpdateMessage = $"New version is out (v{check.LastVersion})";
                _lastCheck = check;
                CanUpdateNow = true;
            }

            CanCheckForUpdate = true;
        }

        public async Task UpdateNow()
        {
            CanUpdateNow = false;
            try
            {
                var shell = (ShellViewModel) Application.Current.MainWindow?.DataContext;
                await _updateManager.Update(_lastCheck, () => shell?.RequestClose());
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating anidow");
            }
            finally
            {
                CanUpdateNow = true;
            }
        }
    }
}