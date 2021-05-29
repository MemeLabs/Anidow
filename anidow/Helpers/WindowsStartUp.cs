using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using File = System.IO.File;

namespace Anidow.Helpers
{
    public static class WindowsStartUp
    {
        private static readonly string StartUpFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        private static readonly string StartUpFile = Path.Combine(StartUpFolder, Application.ProductName + ".lnk");


        public static async Task Enable(Assembly assembly)
        {

            var licenses = assembly.GetManifestResourceNames().Single(p => p.EndsWith("create-shortcut.ps1"));
            await using var stream = assembly.GetManifestResourceStream(licenses);
            using var reader = new StreamReader(stream!);
            var json = await reader.ReadToEndAsync();

            var exeFile = Process.GetCurrentProcess().MainModule.FileName;
            var wd = Path.GetDirectoryName(exeFile);

            json = json.Replace("$args[0]", $"'{exeFile}'");
            json = json.Replace("$args[1]", $"'{StartUpFile}'");
            json = json.Replace("$args[2]", $"'{wd}'");

            var tmpFile = Path.Combine(Path.GetTempPath(), "create-shortcut.ps1");
            await File.WriteAllTextAsync(tmpFile, json);

            var processInfo = new ProcessStartInfo
            {
                FileName = @"powershell.exe",
                Arguments = @"& {"+tmpFile+"}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            //execute powershell command

            //start powershell process using process start info
            var process = new Process {StartInfo = processInfo};
            process.Start();
            await process.WaitForExitAsync();

            File.Delete(tmpFile);
        }
        public static void Disable()
        {
            try
            {
                File.Delete(StartUpFile);
            }
            catch (Exception e)
            {
                // ignore
                Debug.WriteLine(e);
            }
        }
        public static bool IsEnabled()
        {
            return File.Exists(StartUpFile);
        }


    }
}
