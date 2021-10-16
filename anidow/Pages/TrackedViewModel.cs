using Anidow.Events;
using Anidow.Pages.Components.Tracked;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TrackedViewModel : Conductor<IScreen>.StackNavigation
    {
        private readonly TrackedOverViewModel _trackedOverViewModel;

        public TrackedViewModel(TrackedOverViewModel trackedOverViewModel)
        {
            DisplayName = "Tracked";
            _trackedOverViewModel = trackedOverViewModel;
        }

        protected override void OnInitialActivate()
        {
            ActivateItem(_trackedOverViewModel);
        }
    }
}