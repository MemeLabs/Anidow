﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Model;

namespace Anidow.Extensions
{
    public static class AnimeExtension
    {
        public static string GetReleaseGroup(this AnimeBytesTorrentItem item)
        {
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var episodeIndex = parts.FindIndex(p => p.StartsWith("Episode "));
            try
            {
                return parts[episodeIndex - 1];
            }
            catch (Exception )
            {
                return string.Empty;
            }
        }
        public static string GetEpisode(this AnimeBytesTorrentItem item)
        {
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var episodeIndex = parts.FindIndex(p => p.StartsWith("Episode "));
            return episodeIndex == -1 ? string.Empty : parts[episodeIndex][8..].PadLeft(2, '0');
        }
        public static string GetResolution(this AnimeBytesTorrentItem item)
        {
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var resolutionIndex = parts.FindIndex(p => Regex.IsMatch(p, @"\d+p"));
            return resolutionIndex == -1 ? string.Empty : parts[resolutionIndex];
        }
        public static string GetEpisode(this string s)
        {
            var parts = s.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var episodeIndex = parts.FindIndex(p => p.StartsWith("Episode "));
            return episodeIndex == -1 ? string.Empty : parts[episodeIndex][8..].PadLeft(2, '0');
        }


        public static async Task UpdateInDatabase(this Anime anime)
        {
            await using var db = new TrackContext();
            db.Anime.Attach(anime);
            db.Anime.Update(anime);
            await db.SaveChangesAsync();
        }
    }
}