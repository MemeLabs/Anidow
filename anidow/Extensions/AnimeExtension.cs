using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdonisUI.Controls;
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
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string GetReleaseGroup(this AnimeBytesScrapeTorrent item)
        {
            var parts = item.Property.Split('|', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

            try
            {
                return parts[^2];
            }
            catch (Exception)
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

        public static int GetEpisodeInt(this AnimeBytesTorrentItem item)
        {
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

            var episodeIndex = parts.FindIndex(p => p.StartsWith("Episode "));
            try
            {
                var success = int.TryParse(parts[episodeIndex][8..], out var i);
                return success ? i : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string GetResolution(this AnimeBytesTorrentItem item)
        {
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

            var resolutionIndex = parts.FindIndex(p => Regex.IsMatch(p, @"\d+p"));
            return resolutionIndex == -1 ? string.Empty : parts[resolutionIndex];
        }

        public static string GetResolution(this AnimeBytesScrapeTorrent item)
        {
            var parts = item.Property.Split('|', StringSplitOptions.RemoveEmptyEntries)
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

        public static async Task AddToDatabase(this Anime anime, Cover cover = null)
        {
            await using var db = new TrackContext();
            anime.CoverData = null;
            await db.Anime.AddAsync(anime);
            await db.SaveChangesAsync();
            if (cover is not null)
            {
                anime.CoverData = cover;
                await anime.UpdateInDatabase();
            }
        }


        public static async Task<bool> DeleteInDatabase(this Anime anime)
        {
            var result = MessageBox.Show("Are you sure you want to delete this?", anime.Name,
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                return false;
            }

            await using var db = new TrackContext();
            db.Anime.Attach(anime);
            db.Anime.Remove(anime);
            var rows = await db.SaveChangesAsync();
            return rows >= 1;
        }
    }
}