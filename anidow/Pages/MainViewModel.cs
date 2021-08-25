// // Created: 14-06-2021 11:36

using System;
using System.Linq;
using Stylet;

#pragma warning disable 1998

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MainViewModel : Conductor<Screen>.Collection.OneActive
    {
        private readonly AboutViewModel _aboutViewModel;
        private readonly NotifyViewModel _notifyViewModel;
        private readonly IWindowManager _windowManager;

        public MainViewModel(
            HomeViewModel homeViewModel,
            NyaaViewModel nyaaViewModel,
            AnimeBytesViewModel animeBytesViewModel,
            TrackedViewModel trackedViewModel,
            HistoryViewModel historyViewModel,
            AboutViewModel aboutViewModel,
            NotifyViewModel notifyViewModel,
            IWindowManager windowManager)
        {
            Items.Add(homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel)));
            Items.Add(trackedViewModel ?? throw new ArgumentNullException(nameof(trackedViewModel)));
            Items.Add(animeBytesViewModel ?? throw new ArgumentNullException(nameof(animeBytesViewModel)));
            Items.Add(nyaaViewModel ?? throw new ArgumentNullException(nameof(nyaaViewModel)));
            Items.Add(notifyViewModel ?? throw new ArgumentNullException(nameof(notifyViewModel)));
            Items.Add(historyViewModel ?? throw new ArgumentNullException(nameof(historyViewModel)));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _notifyViewModel = notifyViewModel;
        }

        public void OpenAbout()
        {
            _windowManager.ShowDialog(_aboutViewModel);
        }

        protected override void OnInitialActivate()
        {
            ChangeActiveItem(Items.FirstOrDefault(), false);
            _ = _notifyViewModel.Init();
        }
    }
}