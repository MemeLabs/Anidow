using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Anidow.Services
{
    public class AnimeFolderService
    {
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private List<string> _allFiles;

        public AnimeFolderService(ILogger logger, SettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        public List<string> AllFiles() => _allFiles ?? new List<string>();

        private async Task GetAllEpisodes()
        {
            var folder = _settingsService.GetSettings().AnimeFolder;
            if (!Directory.Exists(folder))
            {
                return;
            }

            var files = await Task.Run(() => Directory.GetFiles(folder,
                "*.mkv", SearchOption.AllDirectories));
            _allFiles = new List<string>(files);
        }
    }
}