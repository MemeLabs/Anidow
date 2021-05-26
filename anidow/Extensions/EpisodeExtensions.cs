using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Database.Models;

namespace Anidow.Extensions
{
    public static class EpisodeExtensions
    {
        public static async Task UpdateInDatabase(this Episode episode)
        {
            await using var db = new TrackContext();
            db.Episodes.Attach(episode);
            db.Episodes.Update(episode);
            await db.SaveChangesAsync();
        }

        public static async Task<bool> DeleteInDatabase(this Episode episode)
        {
            await using var db = new TrackContext();
            db.Attach(episode);
            db.Remove(episode);
            var rows = await db.SaveChangesAsync();
            return rows >= 1;
        }
    }
}