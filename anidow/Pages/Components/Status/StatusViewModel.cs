﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Services;
using FluentScheduler;
using Humanizer;
using Newtonsoft.Json.Linq;
using Serilog;
using Stylet;

namespace Anidow.Pages.Components.Status
{
    public class StatusViewModel : Screen
    {
        private readonly NyaaService _nyaaService;
        private readonly AnimeBytesService _animeBytesService;
        private readonly SettingsService _settingsService;
        private readonly AppService _appService;
        private readonly ILogger _logger;

        public StatusViewModel(
            NyaaService nyaaService,
            AnimeBytesService animeBytesService,
            SettingsService settingsService,
            AppService appService,
            ILogger logger)
        {
            _nyaaService = nyaaService;
            _animeBytesService = animeBytesService;
            _settingsService = settingsService;
            _appService = appService;
            _logger = logger;
        }

        public AppState AppState => _appService.State;
        public bool IsOpen { get; set; }
        
        private const string JobNameNyaa = "Rss:Nyaa:Get";
        private const string JobNameAnimeBytesAiring = "Rss:AnimeBytesAiring:Get";
        private const string JobNameAnimeBytesAll = "Rss:AnimeBytesAll:Get";

        public DateTimeOffset LastCheckNyaa { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset NextCheckNyaa { get; set; }
        public DateTimeOffset LastCheckAnimeBytesAiring { get; set; }
        public DateTimeOffset NextCheckAnimeBytesAiring { get; set; }
        public DateTimeOffset LastCheckAnimeBytesAll { get; set; }
        public DateTimeOffset NextCheckAnimeBytesAll { get; set; }

        public bool CanCheckNyaa { get; set; } = true;
        public bool CanCheckAnimeBytesAiring { get; set; } = true;
        public bool CanCheckAnimeBytesAll { get; set; } = true;

        public bool RunningNyaa { get; set; }
        public bool RunningAnimeBytesAiring { get; set; }
        public bool RunningAnimeBytesAll { get; set; }

        public string NextCheckNyaaIn { get; set; } = "00:00";
        public string NextCheckAnimeBytesAiringIn { get; set; } = "00:00";
        public string NextCheckAnimeBytesAllIn { get; set; } = "00:00";

        public bool IsEnabledAnimeBytesAiring =>
            !string.IsNullOrEmpty(_settingsService.Settings.AnimeBytesSettings.PassKey);

        public bool IsEnabledAnimeBytesAll =>
            !string.IsNullOrEmpty(_settingsService.Settings.AnimeBytesSettings.PassKey);

        private Timer _nextCheckTimer;
        public void Init()
        {
            if (_nextCheckTimer is not null)
            {
                return;
            }
            _nextCheckTimer = new Timer(100);
            _nextCheckTimer.Elapsed += NextCheckTimerOnElapsed;
            _nextCheckTimer.Start();
        }
        
        private void NextCheckTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (RunningNyaa && IsOpen ||
                AppState.ShowStatusMiniViewNyaa && RunningNyaa)
            {
                NextCheckNyaaIn = $"{NextCheckNyaa - DateTimeOffset.Now:mm\\:ss}";
            }
            if (RunningAnimeBytesAiring && IsOpen || 
                AppState.ShowStatusMiniViewAnimeBytesAiring && RunningAnimeBytesAiring)
            {
                NextCheckAnimeBytesAiringIn = $"{NextCheckAnimeBytesAiring - DateTimeOffset.Now:mm\\:ss}";
            }
            if (RunningAnimeBytesAll && IsOpen ||
                AppState.ShowStatusMiniViewAnimeBytesAll && RunningAnimeBytesAll)
            {
                NextCheckAnimeBytesAllIn = $"{NextCheckAnimeBytesAll - DateTimeOffset.Now:mm\\:ss}";
            }
        }
        
        public async Task CheckNyaa()
        {
            CanCheckNyaa = false;
            
            await _nyaaService.GetFeedItems(NyaaFilter.NoFilter);

            CanCheckNyaa = true;
        }
        public void PauseNyaa()
        {
            JobManager.RemoveJob(JobNameNyaa);
            RunningNyaa = false;
        }
        public void StartNyaa()
        {
            if(RunningNyaa) return;

            JobManager.AddJob(async () =>
            {
                LastCheckNyaa = DateTimeOffset.Now;

                await CheckNyaa();

                NextCheckNyaa = DateTimeOffset.Now.AddMinutes(_settingsService?.Settings?.RefreshTime ?? 5);

            }, s => s.WithName(JobNameNyaa)
                     .NonReentrant()
                     .ToRunNow()
                     .AndEvery(_settingsService?.Settings?.RefreshTime ?? 5)
                     .Minutes());
            RunningNyaa = true;
        }

        public async Task CheckAnimeBytesAiring()
        {
            CanCheckAnimeBytesAiring = false;
            
            await _animeBytesService.GetFeedItems(AnimeBytesFilter.Airing);

            CanCheckAnimeBytesAiring = true;
        }
        public void PauseAnimeBytesAiring()
        {
            JobManager.RemoveJob(JobNameAnimeBytesAiring);
            RunningAnimeBytesAiring = false;
        }
        public void StartAnimeBytesAiring()
        {
            if (RunningAnimeBytesAiring) return;

            JobManager.AddJob(async () =>
            {
                LastCheckAnimeBytesAiring = DateTimeOffset.Now;

                await CheckAnimeBytesAiring();

                NextCheckAnimeBytesAiring = DateTimeOffset.Now.AddMinutes(5);

            }, s => s.WithName(JobNameAnimeBytesAiring)
                     .NonReentrant()
                     .ToRunNow()
                     .AndEvery(5)
                     .Minutes());

            RunningAnimeBytesAiring = true;
        }

        public async Task CheckAnimeBytesAll()
        {
            CanCheckAnimeBytesAll = false;
            
            await _animeBytesService.GetFeedItems(AnimeBytesFilter.All);

            CanCheckAnimeBytesAll = true;
        }
        public void PauseAnimeBytesAll()
        {
            JobManager.RemoveJob(JobNameAnimeBytesAll);
            RunningAnimeBytesAll = false;
        }
        public void StartAnimeBytesAll()
        {
            if (RunningAnimeBytesAll) return;
            JobManager.AddJob(async () =>
            {
                LastCheckAnimeBytesAll = DateTimeOffset.Now;

                await CheckAnimeBytesAll();

                NextCheckAnimeBytesAll = DateTimeOffset.Now.AddMinutes(5);

            }, s => s.WithName(JobNameAnimeBytesAll)
                     .NonReentrant()
                     .ToRunNow()
                     .AndEvery(5)
                     .Minutes());

            RunningAnimeBytesAll = true;
        }
    }
}