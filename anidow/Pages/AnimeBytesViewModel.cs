using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public AnimeBytesViewModel(AnimeBytesRssViewModel rssViewModel, AnimeBytesSearchViewModel searchViewModel)
        {
            DisplayName = "AnimeBytes";
            Items.Add(searchViewModel);
            Items.Add(rssViewModel);
        }
    }
}