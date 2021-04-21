using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly AnimeBytesRssViewModel _rssViewModel;
        private readonly AnimeBytesSearchViewModel _searchViewModel;
        private IScreen _lastScreen;

        public AnimeBytesViewModel(AnimeBytesRssViewModel rssViewModel, AnimeBytesSearchViewModel searchViewModel)
        {
            _rssViewModel = rssViewModel;
            _searchViewModel = searchViewModel;
            _lastScreen = _searchViewModel;
            DisplayName = "AnimeBytes";
            Items.Add(_searchViewModel);
            Items.Add(_rssViewModel);
        }
    }
}