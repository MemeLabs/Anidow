using System.Windows;
using System.Windows.Controls;
using Anidow.GraphQL;
using Anidow.Utils;

namespace Anidow.Pages.Components.AnimeInfo;

/// <summary>
///     Interaction logic for AnimeInfoPanel.xaml
/// </summary>
public partial class AnimeInfoPanel : UserControl
{
    public AnimeInfoPanel()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var ctx = (AniListAnime)((Button)sender).DataContext;
        ShowImage(ctx.Cover);
    }

    private void ShowImage(string url)
    {
        ImageUtil.ShowImage(url);
    }
}