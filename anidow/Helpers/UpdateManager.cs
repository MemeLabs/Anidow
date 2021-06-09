using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Onova;
using Onova.Models;
using Onova.Services;

#nullable enable

namespace Anidow.Helpers
{
    public class UpdateManager
    {
        private readonly Onova.UpdateManager _updateManager;

        public UpdateManager(HttpClient httpClient)
        {
            var selfContained = false;
#if SELF_CONTAINED && RELEASE
            selfContained = true;
#endif
            _updateManager = new Onova.UpdateManager(
                AssemblyMetadata.FromAssembly(
                    Assembly.GetEntryAssembly()!,
                    Process.GetCurrentProcess().MainModule?.FileName!),
                new GithubPackageResolver(
                    httpClient,
                    "MemeLabs",
                    "Anidow",
                    selfContained ? "anidow-full.zip" : "anidow.zip"),
                new ZipPackageExtractor());

        }

        public async Task<(CheckForUpdatesResult?, bool)> HasUpdate()
        {
            var check = await _updateManager.CheckForUpdatesAsync();
            if (!check.CanUpdate || check.LastVersion == null)
            {
                return (null, false);
            }

            return (check, true);
        }

        public async Task Update(CheckForUpdatesResult? check, Action? closeApp = null, IProgress<double>? progress = null)
        {
            if (check?.LastVersion is null)
            {
                return;
            }

            // Prepare the latest update
            await _updateManager.PrepareUpdateAsync(check.LastVersion, progress);

            // Launch updater and exit
            _updateManager.LaunchUpdater(check.LastVersion);

            if (closeApp is not null)
            {
                closeApp.Invoke();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
