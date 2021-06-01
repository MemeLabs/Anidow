using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Anidow.Helpers;
using Anidow.Model;
using Anidow.Utils;
using Humanizer;
using Newtonsoft.Json;
using Onova.Models;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    public class AboutViewModel : Screen
    {
        private readonly Assembly _assembly;
        private readonly UpdateManager _updateManager;
        private readonly ILogger _logger;
        private CheckForUpdatesResult _lastCheck;

        public AboutViewModel(Assembly assembly, UpdateManager updateManager, ILogger logger)
        {
            _assembly = assembly;
            _updateManager = updateManager;
            _logger = logger;
            DisplayName = "About";
            var licenses = assembly.GetManifestResourceNames().Single(p => p.EndsWith("licenses.json"));
            using var stream = assembly.GetManifestResourceStream(licenses);
            using var reader = new StreamReader(stream!);
            var json = reader.ReadToEnd();
            Packages.AddRange(JsonConvert.DeserializeObject<Package[]>(json));
        }

        public IObservableCollection<Package> Packages { get; } = new BindableCollection<Package>();

        public string Copyright => $"© 2020-{DateTime.Now.Year} MemeLabs";
        public string ProjectUrl => "https://github.com/MemeLabs/Anidow";
        public string AssemblyVersionString => $"{_assembly.GetName().Version} {(Environment.Is64BitProcess ? "(x64)" : "(x32)")}";
        public string Product => "Anidow";
        public string UpdateMessage { get; private set; }
        public bool HasUpdate { get; private set; }

        protected override async void OnInitialActivate()
        {
            await CheckForUpdate();
        }

        public void OpenProjectUrl()
        {
            LinkUtil.Open(ProjectUrl);
        }

        public bool CanCheckForUpdate { get; set; } = true;
        public async Task CheckForUpdate()
        {
            CanCheckForUpdate = false;
            var (check, hasUpdate) = await _updateManager.HasUpdate();
            if (!hasUpdate)
            {
                UpdateMessage = $"You have the latest version";
            }
            else if (check is not null)
            {
                HasUpdate = true;
                UpdateMessage = $"New version is out (v{check.LastVersion})";
                _lastCheck = check;
            }

            CanCheckForUpdate = true;
        }

        public async Task UpdateNow()
        {
            try
            {
                await _updateManager.Update(_lastCheck);
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating anidow");
            }
        }
    }
}