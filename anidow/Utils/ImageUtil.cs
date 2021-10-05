using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Anidow.Pages;
using Stylet;

namespace Anidow.Utils
{
    public static class ImageUtil
    {
        private static Bitmap UriToBitmap(string link)
        {
            var bitmapImage = new BitmapImage(new Uri(link));

            using MemoryStream outStream = new();
            var enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }

        public static Icon UriToIcon(string link) => Icon.FromHandle(UriToBitmap(link).GetHicon());

        public static void ShowImage(ImageSource source)
        {
            var imageView = new ImageView
            {
                Image =
                {
                    Source = source,
                },
                Owner = Application.Current.MainWindow,
            };
            imageView.ShowDialog();
        }

        public static void ShowImage(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var ok = Uri.TryCreate(url, UriKind.Absolute, out var uri);
            if (!ok) return;
            
            var imageView = new ImageView
            {
                Image =
                {
                    Source = new BitmapImage(uri),
                },
                Owner = Application.Current.MainWindow,
            };
            imageView.ShowDialog();
        }
    }
}