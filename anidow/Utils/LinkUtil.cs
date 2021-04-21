using System.Diagnostics;

namespace Anidow.Utils
{
    public static class LinkUtil
    {
        public static void Open(string url)
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}