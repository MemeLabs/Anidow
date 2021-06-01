using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Onova;
using Onova.Models;
using Onova.Services;

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
                    Assembly.GetEntryAssembly(),
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName),
                new GithubPackageResolver(
                    httpClient,
                    "MemeLabs",
                    "Anidow",
                    selfContained ? "anidow-full.zip" : "anidow.zip"),
                new ZipPackageExtractor());

        }

        public async Task<(CheckForUpdatesResult, bool)> HasUpdate()
        {
            var check = await _updateManager.CheckForUpdatesAsync();
            if (!check.CanUpdate || check.LastVersion == null)
            {
                return (null, false);
            }

            return (check, true);
        }

        public async Task Update(CheckForUpdatesResult check)
        {

            // Prepare the latest update
            await _updateManager.PrepareUpdateAsync(check.LastVersion);

            // Launch updater and exit
            _updateManager.LaunchUpdater(check.LastVersion);

            Environment.Exit(0);
        }
    }
}
