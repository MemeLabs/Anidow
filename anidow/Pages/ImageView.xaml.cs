using System;
using System.Windows;

namespace Anidow.Pages;

/// <summary>
///     Interaction logic for ImageView.xaml
/// </summary>
public partial class ImageView
{
    public ImageView()
    {
        InitializeComponent();
    }

    public string Url { get; set; }

    private void ButtonCopy_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(Url);
        }
        catch (Exception)
        {
            //ignore
        }
    }
}