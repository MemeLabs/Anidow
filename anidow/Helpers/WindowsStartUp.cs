using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace Anidow.Helpers
{
    public class WindowsStartUp
    {
        private static readonly string StartUpFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        private static readonly string StartUpFile = Path.Combine(StartUpFolder, Application.ProductName + ".lnk");
        public static void Enable()
        {
            var shell = new WshShell();
            if (shell.CreateShortcut(StartUpFile) is not IWshShortcut shortcut)
            {
                return;
            }

            var exeFile = Process.GetCurrentProcess().MainModule.FileName;
            var wd = Path.GetDirectoryName(exeFile);
            shortcut.TargetPath = exeFile;
            shortcut.WorkingDirectory = wd;
            shortcut.Arguments = "/autostart";
            shortcut.Save();
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
