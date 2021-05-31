using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Anidow.Helpers
{
    public class WindowsStartUp
    {
        private readonly Assembly _assembly;

        public WindowsStartUp(Assembly assembly)
        {
            _assembly = assembly;
        }

        public async Task Enable()
        {
            var startUpFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var startUpFile = Path.Combine(startUpFolder, Application.ProductName + ".lnk");

            var licenses = _assembly.GetManifestResourceNames().Single(p => p.EndsWith("create-shortcut.ps1"));
            await using var stream = _assembly.GetManifestResourceStream(licenses);
            using var reader = new StreamReader(stream!);
            var json = await reader.ReadToEndAsync();

            var exeFile = Process.GetCurrentProcess().MainModule.FileName;
            var wd = Path.GetDirectoryName(exeFile);

            json = json.Replace("$args[0]", $"'{exeFile}'")
                       .Replace("$args[1]", $"'{startUpFile}'")
                       .Replace("$args[2]", $"'{wd}'");

            var tmpFile = Path.Combine(Path.GetTempPath(), "create-shortcut.ps1");
            await File.WriteAllTextAsync(tmpFile, json);

            var processInfo = new ProcessStartInfo
            {
                FileName = @"powershell.exe",
                Arguments = @"& {" + tmpFile + "}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            //execute powershell command

            //start powershell process using process start info
            var process = new Process { StartInfo = processInfo };
            process.Start();
            _ = process.WaitForExitAsync().ContinueWith(_ => File.Delete(tmpFile));
        }

        public void Disable()
        {

            var startUpFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var startUpFile = Path.Combine(startUpFolder, Application.ProductName + ".lnk");
            try
            {
                File.Delete(startUpFile);
            }
            catch (Exception e)
            {
                // ignore
                Debug.WriteLine(e);
            }
        }

        public bool IsEnabled()
        {
            var startUpFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var startUpFile = Path.Combine(startUpFolder, Application.ProductName + ".lnk");
            return File.Exists(startUpFile);
        }
    }
}