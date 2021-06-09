using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Anidow.Database;
using Anidow.Events;
using Anidow.Pages.Components.Tracked;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TrackedViewModel : Conductor<IScreen>.StackNavigation, IHandle<OpenAnimeEditEvent>
    {
        private readonly TrackedOverViewModel _trackedOverViewModel;
        private readonly TrackedAnimeEditContentViewModel _trackedAnimeEditContentViewModel;
        private readonly IEventAggregator _eventAggregator;

        public TrackedViewModel(TrackedOverViewModel trackedOverViewModel,
            TrackedAnimeEditContentViewModel trackedAnimeEditContentViewModel,
            IEventAggregator eventAggregator)
        {
            DisplayName = "Tracked";
            _trackedOverViewModel = trackedOverViewModel;
            _trackedAnimeEditContentViewModel = trackedAnimeEditContentViewModel;
            _eventAggregator = eventAggregator;
        }

        protected override void OnInitialActivate()
        {
            ActivateItem(_trackedOverViewModel);
            _eventAggregator.Subscribe(this);
            
        }

        public void Handle(OpenAnimeEditEvent message)
        {
            _trackedAnimeEditContentViewModel.SetAnime(message.Anime);
            ActivateItem(_trackedAnimeEditContentViewModel);
        }
    }
}