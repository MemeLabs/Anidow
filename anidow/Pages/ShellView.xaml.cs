using Hardcodet.Wpf.TaskbarNotification;
using Jot;
using System;

namespace Anidow.Pages
{
    public partial class ShellView
    {
        public ShellView(Tracker tracker, TaskbarIcon taskbarIcon)
        {
            InitializeComponent();
            TaskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
            tracker.Track(this);
        }
    }
}