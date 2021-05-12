using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Anidow.Utils
{
    public static class ProcessUtil
    {
        private static readonly List<string> _mediaExtensions = new()
        {
            ".MPG", ".MP2", ".MPEG", ".MPE", ".MPV", ".WEBM", ".MKV", ".OGG",
            ".MP4", ".M4P", ".M4V",
            ".AVI", ".FLV", ".WMV"
        };

        public static void OpenFile(string path)
        {
            var file = new FileInfo(path);
            if (!_mediaExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("File extension not allowed");
            }

            new Process
            {
                StartInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                }
            }.Start();
        }

        public static void OpenFolder(string path)
        {
            Process.Start("explorer.exe", path);
        }
    }
}