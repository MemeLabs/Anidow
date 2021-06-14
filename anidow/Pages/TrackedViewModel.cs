using Anidow.Events;
using Anidow.Pages.Components.Tracked;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TrackedViewModel : Conductor<IScreen>.StackNavigation, IHandle<OpenAnimeEditEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly TrackedAnimeEditContentViewModel _trackedAnimeEditContentViewModel;
        private readonly TrackedOverViewModel _trackedOverViewModel;

        public TrackedViewModel(TrackedOverViewModel trackedOverViewModel,
            TrackedAnimeEditContentViewModel trackedAnimeEditContentViewModel,
            IEventAggregator eventAggregator)
        {
            DisplayName = "Tracked";
            _trackedOverViewModel = trackedOverViewModel;
            _trackedAnimeEditContentViewModel = trackedAnimeEditContentViewModel;
            _eventAggregator = eventAggregator;
        }

        public void Handle(OpenAnimeEditEvent message)
        {
            _trackedAnimeEditContentViewModel.SetAnime(message.Anime);
            ActivateItem(_trackedAnimeEditContentViewModel);
        }

        protected override void OnInitialActivate()
        {
            ActivateItem(_trackedOverViewModel);
            _eventAggregator.Subscribe(this);
        }
    }
}