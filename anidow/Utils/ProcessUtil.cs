using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Anidow.Database.Models;

namespace Anidow.Utils
{
    public static class ProcessUtil
    {
        private static readonly List<string> MediaExtensions = new()
        {
            ".MPG", ".MP2", ".MPEG", ".MPE", ".MPV", ".WEBM", ".MKV", ".OGG",
            ".MP4", ".M4P", ".M4V",
            ".AVI", ".FLV", ".WMV",
        };

        public static void OpenFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException($@"file doesn't exist: {path}", nameof(path));
            }

            var file = new FileInfo(path);
            if (!MediaExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($@"File extension not allowed: {file.Extension}");
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                },
            };
            process.Start();
            process.Close();
        }
        public static async Task OpenFile(Episode episode)
        {
            episode.CanOpen = false;
            OpenFile(episode.File);
            await Task.Delay(100);
            episode.CanOpen = true;
        }

        public static bool IsAllowedFile(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var file = new FileInfo(path);
            return IsAllowedFile(file);
        }

        public static bool IsAllowedFile(FileInfo file)
        {
            if (!MediaExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public static void OpenFolder(string path)
        {
            Process.Start("explorer.exe", path);
        }

        public static bool IsRunning(string name)
        {
            var processes = Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(name));

            return processes.Length <= 1;
        }

        public static bool IsRunningMoreThan(string name, int value)
        {
            var processes = Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(name));

            return processes.Length < value;
        }
    }
}