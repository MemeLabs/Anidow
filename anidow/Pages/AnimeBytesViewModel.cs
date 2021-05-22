using System;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public AnimeBytesViewModel(AnimeBytesRssViewModel rssViewModel, AnimeBytesSearchViewModel searchViewModel)
        {
            DisplayName = "AnimeBytes";
            Items.Add(searchViewModel ?? throw new ArgumentNullException(nameof(searchViewModel)));
            Items.Add(rssViewModel ?? throw new ArgumentNullException(nameof(rssViewModel)));
        }
    }
}