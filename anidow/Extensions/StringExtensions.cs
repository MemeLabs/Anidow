using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Anidow.Database.Models;
using Serilog;

namespace Anidow.Extensions
{
    public static class StringExtensions
    {
        public static async Task<Cover> GetCoverData(this string cover, Anime anime, HttpClient httpClient,
            ILogger logger)
        {
            try
            {
                var response = await httpClient.GetAsync(cover);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var mediaType = response.Content.Headers.ContentType.MediaType;
                if (!mediaType.StartsWith("image/"))
                {
                    return null;
                }

                var data = await response.Content.ReadAsByteArrayAsync();
                if (data.Length <= 0)
                {
                    logger.Information($"downloaded cover {cover} but length is 0");
                    return null;
                }

                logger.Information($"downloaded cover {cover}");
                if (!Directory.Exists("covers"))
                {
                    Directory.CreateDirectory("covers");
                }

                var extension = mediaType.Split('/').Last();

                var filePath = Path.Combine("covers", $"{anime.GroupId}.{extension}");
                await File.WriteAllBytesAsync(filePath, data);

                return new Cover
                {
                    File = filePath,
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "failed downloading cover");
                return null;
            }
        }
    }
}