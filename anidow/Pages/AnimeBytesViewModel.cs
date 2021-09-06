using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using FluentScheduler;
using Humanizer;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly AnimeBytesService _service;
        private readonly ILogger _logger;

        public AnimeBytesViewModel(
            AnimeBytesRssViewModel rssViewModel,
            AnimeBytesSearchViewModel searchViewModel,
            AnimeBytesService service,
            ILogger logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Items.Add(searchViewModel ?? throw new ArgumentNullException(nameof(searchViewModel)));
            Items.Add(rssViewModel ?? throw new ArgumentNullException(nameof(rssViewModel)));
            DisplayName = "AnimeBytes";
        }

        public AnimeBytesStats Stats { get; set; } = new();
        public bool StatsLoading { get; set; }

        protected override void OnInitialActivate()
        {
            JobManager.AddJob(async () => await GetStats(), 
                s => s.WithName("AnimeBytes:GetStats")
                     .NonReentrant()
                     .ToRunNow()
                     .AndEvery(5)
                     .Minutes());
        }

        public async Task GetStats()
        {
            StatsLoading = true;
            try
            {
                var stats = await _service.GetStats();
                if (stats is not null && stats.Success)
                {
                    Stats.Yen = $"¥{stats.Stats.Personal.Yen.Now:n0}";
                    Stats.YenPerHour = $"¥{stats.Stats.Personal.Yen.Hour:n0}";
                    Stats.YenPerDay = $"¥{stats.Stats.Personal.Yen.Day:n0}";

                    Stats.HitAndRuns = stats.Stats.Personal.Hnrs.Active;
                    Stats.Class = stats.Stats.Personal.Class;
                }

            }
            catch (Exception e)
            {
                _logger.Error(e, "failed getting AnimeBytes stats");
            }
            StatsLoading = false;
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

    public class AnimeBytesStats : ObservableObject
    {
        public string Yen { get; set; } = "¥0";
        public string YenPerHour { get; set; } = "¥0";
        public string YenPerDay { get; set; } = "¥0";

        public int HitAndRuns { get; set; }

        public string Class { get; set; } = "Unknown";
    }
}