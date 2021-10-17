using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Interfaces;

namespace Anidow.Extensions;

public static class EpisodeExtensions
{
    public static async Task UpdateInDatabase(this IEpisode episode)
    {
        await using var db = new TrackContext();
        db.Episodes.Attach((Episode)episode);
        db.Episodes.Update((Episode)episode);
        await db.SaveChangesAsync();
    }

    public static async Task<bool> DeleteInDatabase(this IEpisode episode)
    {
        await using var db = new TrackContext();
        db.Episodes.Attach((Episode)episode);
        db.Episodes.Remove((Episode)episode);
        var rows = await db.SaveChangesAsync();
        return rows >= 1;
    }
}