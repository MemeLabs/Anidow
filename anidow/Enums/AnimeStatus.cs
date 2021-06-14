namespace Anidow.Enums
{
    public enum AnimeStatus
    {
        Watching = 1,
        Completed = 2,
        Dropped = 4,
        All = Watching | Completed | Dropped,
    }
}