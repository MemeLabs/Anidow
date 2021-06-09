using Anidow.Database.Models;

namespace Anidow.Events
{
    public class TrackedDeleteAnimeEvent
    {
        public Anime Anime { get; set; }
    }
}
