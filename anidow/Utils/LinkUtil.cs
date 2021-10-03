using System.Diagnostics;

namespace Anidow.Utils
{
    public static class LinkUtil
    {
        public static Process Open(string url)
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            };
            return Process.Start(psi);
        }
    }
}