using Anidow.Database.Models;

namespace Anidow.Events;

public class AddToHomeEvent
{
    public Episode Episode { get; init; }
    public Anime Anime { get; set; }
}