using Jot;

namespace Anidow.Pages;

public partial class ShellView
{
    public ShellView(Tracker tracker)
    {
        InitializeComponent();
        tracker.Track(this);
    }
}