using System;
using Humanizer;

namespace Anidow.Model
{
    public class FutureEpisode
    {
        public string Name { get; init; }
        public DateTime Date { get; init; }
        public DateTime DateLocal => Date.ToLocalTime();
        public string DateString => Date.Humanize();
    }
}
